using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using IntegrationTestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.IntegrationTests;

public class IdentityPersistenceTests : IAsyncLifetime
{
    private readonly PostgresTestDatabase _database = new("identity_persistence_tests");

    public Task InitializeAsync()
    {
        return _database.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RegisterLoginAndRefresh_PersistUserAndTokenRotation()
    {
        using var factory = new IdentityPersistenceApiFactory(_database.ConnectionString);
        using var client = factory.CreateClient();
        await factory.MigrateDatabaseAsync();

        var register = await RegisterUserAsync(client);
        var registerJson = await register.Content.ReadFromJsonAsync<JsonElement>();
        var userId = registerJson.GetProperty("user").GetProperty("id").GetGuid();
        var initialRefreshToken = registerJson.GetProperty("refreshToken").GetString();

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            identifier = "seller@example.com",
            password = "Password1!"
        });
        var loginJson = await login.Content.ReadFromJsonAsync<JsonElement>();
        var loginRefreshToken = loginJson.GetProperty("refreshToken").GetString();

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = loginRefreshToken
        });
        var refreshJson = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        var rotatedRefreshToken = refreshJson.GetProperty("refreshToken").GetString();

        register.StatusCode.Should().Be(HttpStatusCode.OK);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        initialRefreshToken.Should().NotBeNullOrWhiteSpace();
        loginRefreshToken.Should().NotBeNullOrWhiteSpace();
        rotatedRefreshToken.Should().NotBeNullOrWhiteSpace();
        rotatedRefreshToken.Should().NotBe(loginRefreshToken);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Id == userId);
        var loginToken = await dbContext.RefreshTokens.SingleAsync(token => token.Token == loginRefreshToken);
        var rotatedToken = await dbContext.RefreshTokens.SingleAsync(token => token.Token == rotatedRefreshToken);

        user.Email.Should().Be("seller@example.com");
        user.PhoneNumber.Should().Be("+79990001010");
        user.Nickname.Should().Be("seller10");
        user.Role.Should().Be(UserRole.User);
        loginToken.IsRevoked.Should().BeTrue();
        loginToken.ReplacedByTokenId.Should().NotBeNull();
        rotatedToken.IsRevoked.Should().BeFalse();
        rotatedToken.FamilyId.Should().Be(loginToken.FamilyId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RefreshTokenReplay_RevokesTokenFamily()
    {
        using var factory = new IdentityPersistenceApiFactory(_database.ConnectionString);
        using var client = factory.CreateClient();
        await factory.MigrateDatabaseAsync();

        var register = await RegisterUserAsync(client, "buyer@example.com", "+79990001011", "buyer11");
        var registerJson = await register.Content.ReadFromJsonAsync<JsonElement>();
        var originalRefreshToken = registerJson.GetProperty("refreshToken").GetString();

        var firstRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = originalRefreshToken
        });
        var replayRefresh = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = originalRefreshToken
        });

        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
        replayRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var originalToken = await dbContext.RefreshTokens.SingleAsync(token => token.Token == originalRefreshToken);
        var tokenFamily = await dbContext.RefreshTokens
            .Where(token => token.FamilyId == originalToken.FamilyId)
            .ToListAsync();

        tokenFamily.Should().NotBeEmpty();
        tokenFamily.Should().OnlyContain(token => token.IsRevoked);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Register_WhenAdminRoleIsRequested_PersistsRegularUser()
    {
        using var factory = new IdentityPersistenceApiFactory(_database.ConnectionString);
        using var client = factory.CreateClient();
        await factory.MigrateDatabaseAsync();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "admin-attempt@example.com",
            phoneNumber = "+79990001012",
            nickname = "admin_attempt",
            password = "Password1!",
            name = "Admin Attempt",
            role = 1
        });

        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await register.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("user").GetProperty("role").GetString().Should().Be("User");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Email == "admin-attempt@example.com");
        user.Role.Should().Be(UserRole.User);
    }

    private static Task<HttpResponseMessage> RegisterUserAsync(
        HttpClient client,
        string email = "seller@example.com",
        string phoneNumber = "+79990001010",
        string nickname = "seller10")
    {
        return client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            phoneNumber,
            nickname,
            password = "Password1!",
            name = "Seller User"
        });
    }

    private sealed class IdentityPersistenceApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public IdentityPersistenceApiFactory(string connectionString)
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
                    ["Jwt:Secret"] = JwtTestTokenFactory.Secret
                });
            });
        }

        public async Task MigrateDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }
}
