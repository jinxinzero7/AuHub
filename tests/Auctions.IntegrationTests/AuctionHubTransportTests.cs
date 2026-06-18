using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auctions.API.Hubs;
using Auctions.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using MassTransit;
using NSubstitute;

namespace Auctions.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AuctionHubTransportCollection
{
    public const string Name = "Auction hub transport";
}

[Collection(AuctionHubTransportCollection.Name)]
public sealed class AuctionHubTransportTests : IAsyncLifetime
{
    private const string JwtSecret = "AuHub_Test_Jwt_Secret_That_Is_Long_Enough_2026";
    private AuctionsHubFactory _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new AuctionsHubFactory();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AuthenticatedClient_NegotiatesAndReceivesClaimBoundUserGroupMessage()
    {
        var userId = Guid.NewGuid();
        var received = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = CreateConnection(CreateToken(userId));
        connection.On<Guid>("UserMessage", value => received.TrySetResult(value));

        await connection.StartAsync();
        await connection.InvokeAsync("JoinUserGroup");

        var hub = _factory.Services.GetRequiredService<IHubContext<AuctionHub>>();
        await hub.Clients.Group($"user-{userId}").SendAsync("UserMessage", userId);

        (await received.Task.WaitAsync(TimeSpan.FromSeconds(5))).Should().Be(userId);
        connection.State.Should().Be(HubConnectionState.Connected);
    }

    [Fact]
    public async Task AnonymousClient_IsRejectedDuringHubConnection()
    {
        await using var connection = CreateConnection(null);

        var connect = () => connection.StartAsync();

        await connect.Should().ThrowAsync<Exception>();
        connection.State.Should().Be(HubConnectionState.Disconnected);
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private HubConnection CreateConnection(string? token) => new HubConnectionBuilder()
        .WithUrl(new Uri(_factory.Server.BaseAddress, "/hubs/auction"), options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            if (token is not null)
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
        })
        .Build();

    private static string CreateToken(Guid userId)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "AuHub",
            audience: "AuHubUsers",
            claims: [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class AuctionsHubFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:Secret", JwtSecret);
            builder.UseSetting("Jwt:Issuer", "AuHub");
            builder.UseSetting("Jwt:Audience", "AuHubUsers");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILotRepository>();
                services.AddSingleton(Substitute.For<ILotRepository>());
                services.AddSingleton(Substitute.For<IPublishEndpoint>());
            });
        }
    }
}
