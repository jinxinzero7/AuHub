using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Identity.IntegrationTests;

public class IdentityApiSmokeTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Health_ReturnsOk()
    {
        using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AdminEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new IdentityApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/users/banned");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class IdentityApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=identity_test;Username=postgres;Password=postgres",
                    ["Jwt:Issuer"] = "AuHub",
                    ["Jwt:Audience"] = "AuHub-Users",
                    ["Jwt:Secret"] = "AuHub_Test_Jwt_Secret_That_Is_Long_Enough_2026"
                });
            });
        }
    }
}
