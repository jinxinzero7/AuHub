using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IntegrationTestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Payment.IntegrationTests;

public class PaymentPersistenceTests : IAsyncLifetime
{
    private readonly PostgresTestDatabase _database = new("payment_persistence_tests");

    public Task InitializeAsync()
    {
        return _database.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    [Fact(Skip = "Requires Docker/Testcontainers. Run manually when Docker is available.")]
    [Trait("Category", "Integration")]
    public async Task TopUp_BalanceAndHistory_PersistInPostgres()
    {
        var userId = Guid.NewGuid();
        using var factory = new PaymentPersistenceApiFactory(_database.ConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestTokenFactory.CreateUserToken(userId));

        var topUp = await client.PostAsJsonAsync("/api/payment/topup", new { Amount = 750m });
        var balance = await client.GetAsync("/api/payment/balance");
        var history = await client.GetAsync("/api/payment/transactions");

        topUp.StatusCode.Should().Be(HttpStatusCode.OK);
        balance.StatusCode.Should().Be(HttpStatusCode.OK);
        history.StatusCode.Should().Be(HttpStatusCode.OK);

        var balanceJson = await balance.Content.ReadFromJsonAsync<JsonElement>();
        balanceJson.GetProperty("balance").GetDecimal().Should().Be(750m);
        balanceJson.GetProperty("frozenBalance").GetDecimal().Should().Be(0m);

        var historyJson = await history.Content.ReadFromJsonAsync<JsonElement>();
        var transaction = historyJson.GetProperty("transactions").EnumerateArray().Should().ContainSingle().Subject;
        transaction.GetProperty("amount").GetDecimal().Should().Be(750m);
        transaction.GetProperty("effect").GetString().Should().Be("AvailableCredit");
    }

    private sealed class PaymentPersistenceApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public PaymentPersistenceApiFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTesting");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString,
                    ["Jwt:Issuer"] = JwtTestTokenFactory.Issuer,
                    ["Jwt:Audience"] = JwtTestTokenFactory.Audience,
                    ["Jwt:Secret"] = JwtTestTokenFactory.Secret,
                    ["InternalApiKey"] = "test-internal-key"
                });
            });
        }
    }
}
