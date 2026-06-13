using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Payment.Application.Repositories;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;

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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task TopUp_ThenBalanceAndHistory_ReturnsDemoWalletState()
    {
        var userId = Guid.NewGuid();
        using var factory = new PaymentApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(userId));

        var topUp = await client.PostAsJsonAsync("/api/payment/topup", new { Amount = 500m });
        var balance = await client.GetAsync("/api/payment/balance");
        var history = await client.GetAsync("/api/payment/transactions");

        topUp.StatusCode.Should().Be(HttpStatusCode.OK);
        balance.StatusCode.Should().Be(HttpStatusCode.OK);
        history.StatusCode.Should().Be(HttpStatusCode.OK);

        var balanceJson = await balance.Content.ReadFromJsonAsync<JsonElement>();
        balanceJson.GetProperty("balance").GetDecimal().Should().Be(500m);
        balanceJson.GetProperty("frozenBalance").GetDecimal().Should().Be(0m);

        var historyJson = await history.Content.ReadFromJsonAsync<JsonElement>();
        var transaction = historyJson.GetProperty("transactions").EnumerateArray().Should().ContainSingle().Subject;
        transaction.GetProperty("amount").GetDecimal().Should().Be(500m);
        transaction.GetProperty("effect").GetString().Should().Be("AvailableCredit");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task TopUpCheckout_WithConfiguredRobokassa_ReturnsPaymentUrl()
    {
        var userId = Guid.NewGuid();
        using var factory = new PaymentApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(userId));

        var response = await client.PostAsJsonAsync("/api/payment/topup/checkout", new { Amount = 500m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("success").GetBoolean().Should().BeTrue();
        payload.GetProperty("provider").GetString().Should().Be("Robokassa");
        payload.GetProperty("isTest").GetBoolean().Should().BeTrue();
        payload.GetProperty("paymentUrl").GetString().Should().Contain("https://auth.robokassa.ru/Merchant/Index.aspx?");
        payload.GetProperty("paymentUrl").GetString().Should().Contain("SignatureValue=");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RobokassaResultCallback_ValidSignature_DepositsIdempotently()
    {
        var userId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        using var factory = new PaymentApiFactory();
        using var client = factory.CreateClient();

        using var firstCallback = CreateRobokassaCallbackContent(userId, operationId, "500.00", "12345");
        var firstResponse = await client.PostAsync("/api/payment/topup/robokassa/result", firstCallback);
        using var secondCallback = CreateRobokassaCallbackContent(userId, operationId, "500.00", "12345");
        var secondResponse = await client.PostAsync("/api/payment/topup/robokassa/result", secondCallback);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await firstResponse.Content.ReadAsStringAsync()).Should().Be("OK12345");
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await secondResponse.Content.ReadAsStringAsync()).Should().Be("OK12345");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(userId));
        var balance = await client.GetAsync("/api/payment/balance");
        var history = await client.GetAsync("/api/payment/transactions");
        var balanceJson = await balance.Content.ReadFromJsonAsync<JsonElement>();
        var historyJson = await history.Content.ReadFromJsonAsync<JsonElement>();

        balanceJson.GetProperty("balance").GetDecimal().Should().Be(500m);
        historyJson.GetProperty("transactions").EnumerateArray().Should().ContainSingle();
    }

    private static string CreateJwt(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "User")
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

    private static FormUrlEncodedContent CreateRobokassaCallbackContent(
        Guid userId,
        Guid operationId,
        string outSum,
        string invoiceId)
    {
        var parameters = new Dictionary<string, string>
        {
            ["OutSum"] = outSum,
            ["InvId"] = invoiceId,
            ["Shp_operationId"] = operationId.ToString(),
            ["Shp_userId"] = userId.ToString()
        };
        parameters["SignatureValue"] = CreateRobokassaCallbackSignature(parameters);
        return new FormUrlEncodedContent(parameters);
    }

    private static string CreateRobokassaCallbackSignature(IReadOnlyDictionary<string, string> parameters)
    {
        var source = $"{parameters["OutSum"]}:{parameters["InvId"]}:password-2:Shp_operationId={parameters["Shp_operationId"]}:Shp_userId={parameters["Shp_userId"]}";
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(source))).ToUpperInvariant();
    }

    private sealed class PaymentApiFactory : WebApplicationFactory<Program>
    {
        private readonly InMemoryPaymentRepository _repository = new();

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
                    ["InternalApiKey"] = "test-internal-key",
                    ["PaymentProviders:Robokassa:MerchantLogin"] = "auhub-demo",
                    ["PaymentProviders:Robokassa:Password1"] = "password-1",
                    ["PaymentProviders:Robokassa:Password2"] = "password-2",
                    ["PaymentProviders:Robokassa:IsTest"] = "true"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IWalletRepository>();
                services.RemoveAll<ITransactionRepository>();
                services.AddSingleton(_repository);
                services.AddSingleton<IWalletRepository>(sp => sp.GetRequiredService<InMemoryPaymentRepository>());
                services.AddSingleton<ITransactionRepository>(sp => sp.GetRequiredService<InMemoryPaymentRepository>());
            });
        }
    }

    private sealed class InMemoryPaymentRepository : IWalletRepository, ITransactionRepository
    {
        private readonly List<Wallet> _wallets = new();
        private readonly List<Transaction> _transactions = new();

        Task<Wallet?> IWalletRepository.GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_wallets.FirstOrDefault(w => w.UserId == userId));
        }

        Task IWalletRepository.AddAsync(Wallet wallet, CancellationToken cancellationToken)
        {
            _wallets.Add(wallet);
            return Task.CompletedTask;
        }

        Task IWalletRepository.SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        Task<List<Transaction>> ITransactionRepository.GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_transactions.Where(t => t.UserId == userId).ToList());
        }

        Task<Transaction?> ITransactionRepository.GetByUserIdTypeAndReferenceIdAsync(
            Guid userId,
            TransactionType type,
            Guid referenceId,
            CancellationToken cancellationToken)
        {
            var transaction = _transactions.FirstOrDefault(t =>
                t.UserId == userId &&
                t.Type == type &&
                t.ReferenceId == referenceId);
            return Task.FromResult(transaction);
        }

        Task ITransactionRepository.AddAsync(Transaction transaction, CancellationToken cancellationToken)
        {
            _transactions.Add(transaction);
            return Task.CompletedTask;
        }

        Task ITransactionRepository.SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
