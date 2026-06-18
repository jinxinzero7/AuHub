using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.IntegrationTests;

public sealed class GatewayRoutePrecedenceTests : IAsyncLifetime
{
    private readonly StubBackend _identity = new("identity");
    private readonly StubBackend _auctions = new("auctions");
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _identity.StartAsync();
        await _auctions.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
                services.AddDataProtection().UseEphemeralDataProtectionProvider());
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:identity-cluster:Destinations:identity-api-1:Address"] = _identity.Address,
                    ["ReverseProxy:Clusters:auctions-cluster:Destinations:auctions-api-1:Address"] = _auctions.Address
                }));
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task AdminUserActivity_UsesAuctionsRouteWithoutIdentityTransform()
    {
        var userId = Guid.NewGuid();

        var response = await _client.GetStringAsync($"/api/admin/users/{userId}/activity");

        response.Should().Be($"auctions:/api/admin/users/{userId}/activity");
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    [InlineData("banned")]
    public async Task OtherAdminUserRequests_UseIdentityRouteAndTransformPath(string suffix)
    {
        var response = await _client.GetStringAsync($"/api/admin/users/{suffix}");

        response.Should().Be($"identity:/api/auth/users/{suffix}");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _identity.DisposeAsync();
        await _auctions.DisposeAsync();
    }

    private sealed class StubBackend(string name) : IAsyncDisposable
    {
        private WebApplication? _application;
        public string Address { get; private set; } = string.Empty;

        public async Task StartAsync()
        {
            var port = GetFreeTcpPort();
            Address = $"http://127.0.0.1:{port}";

            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls(Address);
            _application = builder.Build();
            _application.MapFallback(context => context.Response.WriteAsync($"{name}:{context.Request.Path}"));
            await _application.StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_application is not null)
                await _application.DisposeAsync();
        }

        private static int GetFreeTcpPort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }
}
