using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Notifications.IntegrationTests;

public class NotificationsApiSmokeTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Health_ReturnsOk()
    {
        using var factory = new NotificationsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task NotificationsList_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new NotificationsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InternalSend_WithoutInternalKey_ReturnsUnauthorized()
    {
        using var factory = new NotificationsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/notifications/send", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class NotificationsApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=notifications_test;Username=postgres;Password=postgres",
                    ["Jwt:Issuer"] = "AuHub",
                    ["Jwt:Audience"] = "AuHub-Users",
                    ["Jwt:Secret"] = "AuHub_Test_Jwt_Secret_That_Is_Long_Enough_2026",
                    ["InternalApiKey"] = "test-internal-key"
                });
            });
        }
    }
}
