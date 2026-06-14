using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AuHub.Shared.Contracts;
using FluentAssertions;
using IntegrationTestSupport;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifications.API.Consumers;
using Notifications.Application.Commands.SendNotification;
using Notifications.Domain.Enums;
using Notifications.Infrastructure.Data;
using NSubstitute;

namespace Notifications.IntegrationTests;

public class NotificationsPersistenceTests : IAsyncLifetime
{
    private const string InternalApiKey = "test-internal-key";

    private readonly PostgresTestDatabase _database = new("notifications_persistence_tests");
    private NotificationsPersistenceFactory _factory = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        _factory = new NotificationsPersistenceFactory(_database.ConnectionString);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task NotificationFlow_PersistsSendListUnreadAndMarkAsRead()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Internal-Api-Key", InternalApiKey);
        var sendResponse = await client.PostAsJsonAsync("/api/notifications/send", new
        {
            userId,
            type = 0,
            title = "New bid",
            message = "A new bid was placed on your lot"
        });

        sendResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var notificationId = await ReadGuidAsync(sendResponse);

        client.DefaultRequestHeaders.Remove("X-Internal-Api-Key");
        Authenticate(client, userId);
        var listResponse = await client.GetAsync("/api/notifications?page=1&pageSize=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listJson = await JsonNode.ParseAsync(await listResponse.Content.ReadAsStreamAsync());
        listJson!["totalCount"]!.GetValue<int>().Should().Be(1);
        var firstNotification = listJson["notifications"]!.AsArray().Single()!;
        firstNotification["id"]!.GetValue<string>().Should().Be(notificationId.ToString());
        firstNotification["isRead"]!.GetValue<bool>().Should().BeFalse();

        var unreadResponse = await client.GetAsync("/api/notifications/unread-count");
        unreadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unreadCount = await unreadResponse.Content.ReadFromJsonAsync<int>();
        unreadCount.Should().Be(1);

        Authenticate(client, otherUserId);
        var otherUserMarkResponse = await client.PostAsync($"/api/notifications/{notificationId}/read", null);
        otherUserMarkResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Authenticate(client, userId);
        var markResponse = await client.PostAsync($"/api/notifications/{notificationId}/read", null);
        markResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var unreadAfterReadResponse = await client.GetAsync("/api/notifications/unread-count");
        unreadAfterReadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unreadAfterRead = await unreadAfterReadResponse.Content.ReadFromJsonAsync<int>();
        unreadAfterRead.Should().Be(0);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var notification = await dbContext.Notifications.AsNoTracking().SingleAsync(n => n.Id == notificationId);
        notification.UserId.Should().Be(userId);
        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BidPlacedConsumer_CreatesNewBidNotificationForSeller()
    {
        var sellerId = Guid.NewGuid();

        await using var scope = _factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<SendNotificationCommandHandler>();
        var consumer = new BidPlacedConsumer(handler, Substitute.For<ILogger<BidPlacedConsumer>>());
        var context = Substitute.For<ConsumeContext<BidPlacedEvent>>();
        context.Message.Returns(new BidPlacedEvent
        {
            LotId = Guid.NewGuid(),
            BidderId = Guid.NewGuid(),
            BidderName = "Buyer",
            Amount = 1500m,
            SellerId = sellerId,
            LotTitle = "Vintage camera"
        });

        await consumer.Consume(context);

        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var notification = await dbContext.Notifications.AsNoTracking().SingleAsync(n => n.UserId == sellerId);
        notification.Type.Should().Be(NotificationType.NewBid);
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AuctionCompletedConsumer_WithWinner_CreatesWonAuctionNotification()
    {
        var winnerId = Guid.NewGuid();

        await using var scope = _factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<SendNotificationCommandHandler>();
        var consumer = new AuctionCompletedConsumer(handler, Substitute.For<ILogger<AuctionCompletedConsumer>>());
        var context = Substitute.For<ConsumeContext<AuctionCompletedEvent>>();
        context.Message.Returns(new AuctionCompletedEvent
        {
            LotId = Guid.NewGuid(),
            LotTitle = "Mechanical keyboard",
            WinnerId = winnerId,
            WinnerName = "Winner",
            FinalPrice = 2500m,
            SellerId = Guid.NewGuid()
        });

        await consumer.Consume(context);

        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var notification = await dbContext.Notifications.AsNoTracking().SingleAsync(n => n.UserId == winnerId);
        notification.Type.Should().Be(NotificationType.WonAuction);
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AuctionCompletedConsumer_WithoutWinner_DoesNotCreateNotification()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<SendNotificationCommandHandler>();
        var consumer = new AuctionCompletedConsumer(handler, Substitute.For<ILogger<AuctionCompletedConsumer>>());
        var context = Substitute.For<ConsumeContext<AuctionCompletedEvent>>();
        context.Message.Returns(new AuctionCompletedEvent
        {
            LotId = Guid.NewGuid(),
            LotTitle = "No bid lot",
            WinnerId = null,
            FinalPrice = 1200m,
            SellerId = Guid.NewGuid()
        });

        await consumer.Consume(context);

        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var notificationsCount = await dbContext.Notifications.CountAsync();
        notificationsCount.Should().Be(0);
    }

    private static void Authenticate(HttpClient client, Guid userId, string role = "User")
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestTokenFactory.CreateUserToken(userId, role));
    }

    private static async Task<Guid> ReadGuidAsync(HttpResponseMessage response)
    {
        var json = await JsonNode.ParseAsync(await response.Content.ReadAsStreamAsync());
        return Guid.Parse(json!.GetValue<string>());
    }

    private sealed class NotificationsPersistenceFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public NotificationsPersistenceFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString,
                    ["Jwt:Issuer"] = JwtTestTokenFactory.Issuer,
                    ["Jwt:Audience"] = JwtTestTokenFactory.Audience,
                    ["Jwt:Secret"] = JwtTestTokenFactory.Secret,
                    ["InternalApiKey"] = InternalApiKey
                });
            });
        }
    }
}
