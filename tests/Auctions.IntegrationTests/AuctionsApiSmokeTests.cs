using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Auctions.IntegrationTests;

public class AuctionsApiSmokeTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Health_ReturnsOk()
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateLot_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/lots", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PendingModeration_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/lots/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class AuctionsApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=auctions_test;Username=postgres;Password=postgres",
                    ["Jwt:Issuer"] = "AuHub",
                    ["Jwt:Audience"] = "AuHub-Users",
                    ["Jwt:Secret"] = "AuHub_Test_Jwt_Secret_That_Is_Long_Enough_2026",
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
                services.AddSingleton(Substitute.For<IPublishEndpoint>());
            });
        }
    }
}
