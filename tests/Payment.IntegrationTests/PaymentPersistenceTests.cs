using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AuHub.Shared.Security;
using FluentAssertions;
using IntegrationTestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Infrastructure.Data;

namespace Payment.IntegrationTests;

public class PaymentPersistenceTests : IAsyncLifetime
{
    private const string InternalApiKeyValue = "test-internal-key";
    private readonly PostgresTestDatabase _database = new("payment_persistence_tests");

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
    public async Task TopUp_BalanceAndHistory_PersistInPostgres()
    {
        var userId = Guid.NewGuid();
        using var factory = new PaymentPersistenceApiFactory(_database.ConnectionString);
        using var client = factory.CreateClient();
        await factory.MigrateDatabaseAsync();
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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReserveAndRelease_PersistFrozenBalanceTransitions()
    {
        var userId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        using var factory = new PaymentPersistenceApiFactory(_database.ConnectionString);
        using var client = factory.CreateClient();
        await factory.MigrateDatabaseAsync();
        await TopUpAsync(client, userId, 1000m);

        var reserve = await PostInternalAsync(client, "/api/payment/reserve", new
        {
            userId,
            amount = 400m,
            lotId
        });
        var balanceAfterReserve = await GetBalanceAsync(client, userId);
        var release = await PostInternalAsync(client, "/api/payment/release", new
        {
            userId,
            amount = 400m,
            lotId
        });
        var balanceAfterRelease = await GetBalanceAsync(client, userId);
        var history = await GetTransactionsAsync(client, userId);

        reserve.StatusCode.Should().Be(HttpStatusCode.OK);
        balanceAfterReserve.GetProperty("balance").GetDecimal().Should().Be(600m);
        balanceAfterReserve.GetProperty("frozenBalance").GetDecimal().Should().Be(400m);
        release.StatusCode.Should().Be(HttpStatusCode.OK);
        balanceAfterRelease.GetProperty("balance").GetDecimal().Should().Be(1000m);
        balanceAfterRelease.GetProperty("frozenBalance").GetDecimal().Should().Be(0m);
        history.GetProperty("transactions").EnumerateArray().Select(t => t.GetProperty("effect").GetString())
            .Should().Contain(["AvailableCredit", "Freeze", "Release"]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ChargeWinnerAndRefund_PersistEscrowMoneyFlow()
    {
        var userId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        using var factory = new PaymentPersistenceApiFactory(_database.ConnectionString);
        using var client = factory.CreateClient();
        await factory.MigrateDatabaseAsync();
        await TopUpAsync(client, userId, 1000m);
        await PostInternalAsync(client, "/api/payment/reserve", new { userId, amount = 400m, lotId });

        var charge = await PostInternalAsync(client, "/api/payment/charge-winner", new
        {
            userId,
            amount = 400m,
            lotId
        });
        var balanceAfterCharge = await GetBalanceAsync(client, userId);
        var refund = await PostInternalAsync(client, "/api/payment/refund", new
        {
            userId,
            amount = 400m,
            lotId
        });
        var balanceAfterRefund = await GetBalanceAsync(client, userId);

        charge.StatusCode.Should().Be(HttpStatusCode.OK);
        balanceAfterCharge.GetProperty("balance").GetDecimal().Should().Be(600m);
        balanceAfterCharge.GetProperty("frozenBalance").GetDecimal().Should().Be(0m);
        refund.StatusCode.Should().Be(HttpStatusCode.OK);
        balanceAfterRefund.GetProperty("balance").GetDecimal().Should().Be(1000m);
        balanceAfterRefund.GetProperty("frozenBalance").GetDecimal().Should().Be(0m);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task TransferToSeller_PersistsSellerPayoutAndPlatformFee()
    {
        var sellerId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        using var factory = new PaymentPersistenceApiFactory(_database.ConnectionString);
        using var client = factory.CreateClient();
        await factory.MigrateDatabaseAsync();

        var transfer = await PostInternalAsync(client, "/api/payment/transfer-seller", new
        {
            userId = sellerId,
            amount = 990m,
            serviceFee = 10m,
            lotId
        });
        var sellerBalance = await GetBalanceAsync(client, sellerId);
        var platformBalance = await GetBalanceAsync(client, new Guid("00000000-0000-0000-0000-000000000001"));

        transfer.StatusCode.Should().Be(HttpStatusCode.OK);
        sellerBalance.GetProperty("balance").GetDecimal().Should().Be(990m);
        sellerBalance.GetProperty("frozenBalance").GetDecimal().Should().Be(0m);
        platformBalance.GetProperty("balance").GetDecimal().Should().Be(10m);
        platformBalance.GetProperty("frozenBalance").GetDecimal().Should().Be(0m);
    }

    private static async Task TopUpAsync(HttpClient client, Guid userId, decimal amount)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestTokenFactory.CreateUserToken(userId));
        var response = await client.PostAsJsonAsync("/api/payment/topup", new { Amount = amount });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> PostInternalAsync(HttpClient client, string url, object payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add(InternalApiKey.HeaderName, InternalApiKeyValue);

        return await client.SendAsync(request);
    }

    private static async Task<JsonElement> GetBalanceAsync(HttpClient client, Guid userId)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestTokenFactory.CreateUserToken(userId));
        var response = await client.GetAsync("/api/payment/balance");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> GetTransactionsAsync(HttpClient client, Guid userId)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestTokenFactory.CreateUserToken(userId));
        var response = await client.GetAsync("/api/payment/transactions");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
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
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString,
                    ["Jwt:Issuer"] = JwtTestTokenFactory.Issuer,
                    ["Jwt:Audience"] = JwtTestTokenFactory.Audience,
                    ["Jwt:Secret"] = JwtTestTokenFactory.Secret,
                    ["InternalApiKey"] = InternalApiKeyValue
                });
            });
        }

        public async Task MigrateDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            await dbContext.Database.MigrateAsync();
        }
    }
}
