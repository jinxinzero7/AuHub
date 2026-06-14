using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Infrastructure.Data;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using IntegrationTestSupport;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Auctions.IntegrationTests;

public class AuctionsPersistenceTests : IAsyncLifetime
{
    private readonly PostgresTestDatabase _database = new("auctions_persistence_tests");
    private AuctionsPersistenceFactory _factory = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        _factory = new AuctionsPersistenceFactory(_database.ConnectionString);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task LotLifecycle_PersistsDraftModerationApprovalAndPublicListing()
    {
        var sellerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Vintage camera Zenit");

        var createdLot = await LoadLotAsync(lotId);
        createdLot.Status.Should().Be(LotStatus.Draft);
        createdLot.SellerId.Should().Be(sellerId);
        createdLot.CurrentPrice.Amount.Should().Be(1200m);
        createdLot.SupportedDeliveryProviders.Should().BeEquivalentTo([DeliveryProvider.Cdek, DeliveryProvider.RussianPost]);

        Authenticate(client, otherUserId);
        var otherUserSubmitResponse = await client.PostAsync($"/api/lots/{lotId}/submit-for-moderation", null);
        otherUserSubmitResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Authenticate(client, sellerId);
        var submitResponse = await client.PostAsync($"/api/lots/{lotId}/submit-for-moderation", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pendingLot = await LoadLotAsync(lotId);
        pendingLot.Status.Should().Be(LotStatus.PendingModeration);

        var editWhilePendingResponse = await client.PutAsJsonAsync($"/api/lots/{lotId}", new
        {
            title = "Edited while pending",
            description = "This change must not be persisted",
            startingPrice = 1500m,
            durationHours = 24,
            supportedDeliveryProviders = new[] { "Cdek" },
            submitForModeration = false
        });

        editWhilePendingResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var unchangedPendingLot = await LoadLotAsync(lotId);
        unchangedPendingLot.Title.Should().Be("Vintage camera Zenit");
        unchangedPendingLot.Status.Should().Be(LotStatus.PendingModeration);

        Authenticate(client, adminId, "Admin");
        var approveResponse = await client.PostAsync($"/api/lots/{lotId}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var activeLot = await LoadLotAsync(lotId);
        activeLot.Status.Should().Be(LotStatus.Active);
        activeLot.StartTime.Should().NotBeNull();
        activeLot.EndTime.Should().NotBeNull();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
            var auditLogExists = await dbContext.AdminAuditLogs
                .AnyAsync(log =>
                    log.ActorUserId == adminId &&
                    log.Action == "LotApprove" &&
                    log.TargetId == lotId);

            auditLogExists.Should().BeTrue();
        }

        client.DefaultRequestHeaders.Authorization = null;
        var publicResponse = await client.GetAsync("/api/lots?onlyActive=true&page=1&pageSize=20");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicJson = await JsonNode.ParseAsync(await publicResponse.Content.ReadAsStreamAsync());
        var lots = publicJson?["lots"]?.AsArray();
        lots.Should().NotBeNull();
        lots!.Select(node => Guid.Parse(node!["id"]!.GetValue<string>()))
            .Should()
            .Contain(lotId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RejectedLot_CanBeEditedBackToDraftAndPersistsAuditLog()
    {
        var sellerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Porcelain tea set");

        Authenticate(client, sellerId);
        var submitResponse = await client.PostAsync($"/api/lots/{lotId}/submit-for-moderation", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Authenticate(client, adminId, "Admin");
        var rejectResponse = await client.PostAsJsonAsync($"/api/lots/{lotId}/reject", new
        {
            reason = "Photos do not show the item condition"
        });

        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rejectedLot = await LoadLotAsync(lotId);
        rejectedLot.Status.Should().Be(LotStatus.Rejected);
        rejectedLot.AdminComment.Should().Be("Photos do not show the item condition");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
            var auditLogExists = await dbContext.AdminAuditLogs
                .AnyAsync(log =>
                    log.ActorUserId == adminId &&
                    log.Action == "LotReject" &&
                    log.TargetId == lotId &&
                    log.Details == "Photos do not show the item condition");

            auditLogExists.Should().BeTrue();
        }

        Authenticate(client, sellerId);
        var editResponse = await client.PutAsJsonAsync($"/api/lots/{lotId}", new
        {
            title = "Porcelain tea set with detailed photos",
            description = "Updated description with condition details and complete photo set",
            startingPrice = 2200m,
            durationHours = 72,
            supportedDeliveryProviders = new[] { "YandexDelivery", "RussianPost" },
            submitForModeration = false
        });

        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var editedLot = await LoadLotAsync(lotId);
        editedLot.Status.Should().Be(LotStatus.Draft);
        editedLot.AdminComment.Should().BeNull();
        editedLot.Title.Should().Be("Porcelain tea set with detailed photos");
        editedLot.CurrentPrice.Amount.Should().Be(2200m);
        editedLot.SupportedDeliveryProviders.Should().BeEquivalentTo([DeliveryProvider.YandexDelivery, DeliveryProvider.RussianPost]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PlaceBid_OnActiveLot_ReservesFundsAndPersistsBidAndOutbox()
    {
        var sellerId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        var lotId = await CreateDraftLotAsync(client, sellerId, "Mechanical keyboard");
        await SubmitAndApproveLotAsync(client, lotId, sellerId, adminId);

        Authenticate(client, bidderId);
        var bidResponse = await client.PostAsJsonAsync($"/api/lots/{lotId}/bids", new
        {
            amount = 1500m,
            idempotencyKey = Guid.NewGuid()
        });

        var bidBody = await bidResponse.Content.ReadAsStringAsync();
        bidResponse.StatusCode.Should().Be(HttpStatusCode.OK, bidBody);

        var lot = await LoadLotAsync(lotId);
        lot.CurrentPrice.Amount.Should().Be(1500m);
        lot.WinnerId.Should().BeNull();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        var bid = await dbContext.Bids.AsNoTracking().SingleAsync(savedBid => savedBid.LotId == lotId);
        bid.BidderId.Should().Be(bidderId);
        bid.Amount.Amount.Should().Be(1500m);

        var bidPlacedOutboxExists = await dbContext.OutboxMessages
            .AnyAsync(message => message.Type == "BidPlaced" && message.Payload.Contains(lotId.ToString()));
        bidPlacedOutboxExists.Should().BeTrue();

        _factory.PaymentClient.ReservedFunds.Should().ContainSingle(reservation =>
            reservation.UserId == bidderId &&
            reservation.Amount == 1500m &&
            reservation.LotId == lotId);
    }

    private async Task<Guid> CreateDraftLotAsync(HttpClient client, Guid sellerId, string title)
    {
        Authenticate(client, sellerId);
        var createResponse = await client.PostAsJsonAsync("/api/lots", new
        {
            title,
            description = "Working film camera with original leather case",
            startingPrice = 1200m,
            durationHours = 48,
            supportedDeliveryProviders = new[] { "Cdek", "RussianPost" }
        });

        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createBody);
        return await ReadGuidAsync(createResponse, "lotId");
    }

    private static async Task SubmitAndApproveLotAsync(HttpClient client, Guid lotId, Guid sellerId, Guid adminId)
    {
        Authenticate(client, sellerId);
        var submitResponse = await client.PostAsync($"/api/lots/{lotId}/submit-for-moderation", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        Authenticate(client, adminId, "Admin");
        var approveResponse = await client.PostAsync($"/api/lots/{lotId}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Lot> LoadLotAsync(Guid lotId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        return await dbContext.Lots
            .AsNoTracking()
            .SingleAsync(lot => lot.Id == lotId);
    }

    private static void Authenticate(HttpClient client, Guid userId, string role = "User")
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestTokenFactory.CreateUserToken(userId, role));
    }

    private static async Task<Guid> ReadGuidAsync(HttpResponseMessage response, string propertyName)
    {
        var json = await JsonNode.ParseAsync(await response.Content.ReadAsStreamAsync());
        return Guid.Parse(json![propertyName]!.GetValue<string>());
    }

    private sealed class AuctionsPersistenceFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public FakePaymentClient PaymentClient { get; } = new();

        public AuctionsPersistenceFactory(string connectionString)
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
                    ["MinIO:Endpoint"] = "localhost:9000",
                    ["MinIO:ExternalEndpoint"] = "http://localhost:9000",
                    ["MinIO:AccessKey"] = "test",
                    ["MinIO:SecretKey"] = "test",
                    ["MinIO:BucketName"] = "test",
                    ["DisableHostedServices"] = "true",
                    ["Payment:BaseUrl"] = "http://localhost",
                    ["Notifications:BaseUrl"] = "http://localhost"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEventPublisher>();
                services.RemoveAll<INotificationClient>();
                services.RemoveAll<IImageStorageService>();
                services.RemoveAll<IPaymentClient>();

                services.AddSingleton<IEventPublisher, NoopEventPublisher>();
                services.AddSingleton<INotificationClient, NoopNotificationClient>();
                services.AddSingleton<IImageStorageService, NoopImageStorageService>();
                services.AddSingleton<IPaymentClient>(PaymentClient);
                services.AddSingleton(Substitute.For<IPublishEndpoint>());
            });
        }
    }

    private sealed class FakePaymentClient : IPaymentClient
    {
        public List<FundsReservation> ReservedFunds { get; } = new();

        public Task<BalanceResult> GetBalanceAsync(Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult(new BalanceResult(true, 100_000m));
        }

        public Task<PaymentResult> ReserveFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default)
        {
            ReservedFunds.Add(new FundsReservation(userId, amount, lotId));
            return Task.FromResult(new PaymentResult(true));
        }

        public Task<PaymentResult> ReleaseFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default)
        {
            return Task.FromResult(new PaymentResult(true));
        }

        public Task<PaymentResult> ChargeWinnerAsync(Guid winnerId, decimal amount, Guid lotId, CancellationToken ct = default)
        {
            return Task.FromResult(new PaymentResult(true));
        }

        public Task<PaymentResult> TransferToSellerAsync(Guid sellerId, decimal amount, decimal serviceFee, Guid lotId, CancellationToken ct = default)
        {
            return Task.FromResult(new PaymentResult(true));
        }

        public Task<PaymentResult> RefundFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default)
        {
            return Task.FromResult(new PaymentResult(true));
        }
    }

    private sealed record FundsReservation(Guid UserId, decimal Amount, Guid LotId);

    private sealed class NoopEventPublisher : IEventPublisher
    {
        public Task PublishNewBidAsync(Guid lotId, decimal newPrice, string bidderName, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishLotCompletedAsync(Guid lotId, string title, decimal finalPrice, string? winnerName, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishUserNotificationAsync(Guid userId, string type, string message, Guid? lotId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopNotificationClient : INotificationClient
    {
        public Task SendNotificationAsync(Guid userId, NotificationType type, string title, string message, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class NoopImageStorageService : IImageStorageService
    {
        public Task<string> UploadAsync(Stream fileStream, string objectName, string contentType, CancellationToken ct = default) => Task.FromResult(objectName);

        public Task<string> GetPresignedUrlAsync(string objectName, int expiresMinutes = 60, CancellationToken ct = default) => Task.FromResult($"http://localhost/{objectName}");

        public Task<(Stream Stream, string ContentType, long Size)> GetStreamAsync(string objectName, CancellationToken ct = default)
        {
            Stream stream = new MemoryStream();
            return Task.FromResult((stream, "application/octet-stream", 0L));
        }

        public Task DeleteAsync(string objectName, CancellationToken ct = default) => Task.CompletedTask;

        public Task InitializeBucketAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
