using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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

    [Theory]
    [InlineData("/api/lots")]
    [InlineData("/api/lots/00000000-0000-0000-0000-000000000001/bids")]
    [Trait("Category", "Integration")]
    public async Task MarketplaceMutation_WithAdminToken_ReturnsForbidden(string route)
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwt(Guid.NewGuid(), "Admin"));

        var response = await client.PostAsJsonAsync(route, new { Amount = 1500m });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AdminRead_WithUserToken_ReturnsForbidden()
    {
        using var factory = new AuctionsApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwt(Guid.NewGuid(), "User"));

        var response = await client.GetAsync("/api/admin/lots/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static string CreateJwt(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("AuHub_Test_Jwt_Secret_That_Is_Long_Enough_2026"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "AuHub",
            audience: "AuHub-Users",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
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
