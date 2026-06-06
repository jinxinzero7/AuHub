using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Payment.IntegrationTests;

public class PaymentApiSmokeTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Health_ReturnsOk()
    {
        using var factory = new PaymentApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PublicBalance_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new PaymentApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/payment/balance");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InternalReserve_WithoutInternalKey_ReturnsUnauthorized()
    {
        using var factory = new PaymentApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/payment/reserve", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class PaymentApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=payment_test;Username=postgres;Password=postgres",
                    ["Jwt:Issuer"] = "AuHub",
                    ["Jwt:Audience"] = "AuHub-Users",
                    ["Jwt:Secret"] = "AuHub_Test_Jwt_Secret_That_Is_Long_Enough_2026",
                    ["InternalApiKey"] = "test-internal-key"
                });
            });
        }
    }
}
