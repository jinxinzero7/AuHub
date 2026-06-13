using System.Security.Cryptography;
using System.Text;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using Payment.Infrastructure.Services;

namespace Payment.IntegrationTests;

public class RobokassaPaymentCheckoutProviderTests
{
    [Fact]
    public void CreateTopUpCheckout_ConfiguredProvider_ReturnsSignedTestPaymentUrl()
    {
        var provider = CreateProvider();
        var userId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        var result = provider.CreateTopUpCheckout(userId, Money.FromDecimal(500m), operationId, 12345);

        result.IsSuccess.Should().BeTrue();
        var checkout = result.Value;
        checkout.Provider.Should().Be("Robokassa");
        checkout.OperationId.Should().Be(operationId);
        checkout.InvoiceId.Should().Be(12345);
        checkout.IsTest.Should().BeTrue();
        checkout.PaymentUrl.Should().Contain("https://auth.robokassa.ru/Merchant/Index.aspx?");
        checkout.PaymentUrl.Should().Contain("MerchantLogin=auhub-demo");
        checkout.PaymentUrl.Should().Contain("OutSum=500.00");
        checkout.PaymentUrl.Should().Contain("InvId=12345");
        checkout.PaymentUrl.Should().Contain("IsTest=1");
        checkout.PaymentUrl.Should().Contain($"Shp_operationId={Uri.EscapeDataString(operationId.ToString())}");
        checkout.PaymentUrl.Should().Contain($"Shp_userId={Uri.EscapeDataString(userId.ToString())}");
        checkout.PaymentUrl.Should().Contain($"SignatureValue={CreatePaymentSignature("500.00", 12345, operationId, userId)}");
    }

    [Fact]
    public void ConfirmTopUpCallback_ValidSignature_ReturnsConfirmation()
    {
        var provider = CreateProvider();
        var userId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var parameters = new Dictionary<string, string>
        {
            ["OutSum"] = "500.00",
            ["InvId"] = "12345",
            ["Shp_operationId"] = operationId.ToString(),
            ["Shp_userId"] = userId.ToString()
        };
        parameters["SignatureValue"] = CreateCallbackSignature(parameters);

        var result = provider.ConfirmTopUpCallback(parameters);

        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be("Robokassa");
        result.Value.OperationId.Should().Be(operationId);
        result.Value.UserId.Should().Be(userId);
        result.Value.InvoiceId.Should().Be(12345);
        result.Value.Amount.Should().Be(Money.FromDecimal(500m));
    }

    [Fact]
    public void ConfirmTopUpCallback_InvalidSignature_ReturnsFailure()
    {
        var provider = CreateProvider();
        var parameters = new Dictionary<string, string>
        {
            ["OutSum"] = "500.00",
            ["InvId"] = "12345",
            ["Shp_operationId"] = Guid.NewGuid().ToString(),
            ["Shp_userId"] = Guid.NewGuid().ToString(),
            ["SignatureValue"] = "BAD"
        };

        var result = provider.ConfirmTopUpCallback(parameters);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid Robokassa signature");
    }

    private static RobokassaPaymentCheckoutProvider CreateProvider()
    {
        return new RobokassaPaymentCheckoutProvider(new RobokassaOptions
        {
            MerchantLogin = "auhub-demo",
            Password1 = "password-1",
            Password2 = "password-2",
            IsTest = true
        });
    }

    private static string CreatePaymentSignature(string outSum, long invoiceId, Guid operationId, Guid userId)
    {
        return Md5Hex($"auhub-demo:{outSum}:{invoiceId}:password-1:Shp_operationId={operationId}:Shp_userId={userId}");
    }

    private static string CreateCallbackSignature(IReadOnlyDictionary<string, string> parameters)
    {
        return Md5Hex($"{parameters["OutSum"]}:{parameters["InvId"]}:password-2:Shp_operationId={parameters["Shp_operationId"]}:Shp_userId={parameters["Shp_userId"]}");
    }

    private static string Md5Hex(string source)
    {
        return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(source))).ToUpperInvariant();
    }
}
