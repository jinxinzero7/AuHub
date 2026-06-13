using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Payment.Application.Services;

namespace Payment.Infrastructure.Services;

public class RobokassaPaymentCheckoutProvider : IPaymentCheckoutProvider
{
    private readonly RobokassaOptions _options;

    public RobokassaPaymentCheckoutProvider(RobokassaOptions options)
    {
        _options = options;
    }

    public string ProviderName => "Robokassa";

    public Result<PaymentCheckout> CreateTopUpCheckout(
        Guid userId,
        Money amount,
        Guid operationId,
        long invoiceId)
    {
        if (!_options.IsConfigured)
            return Result.Failure<PaymentCheckout>("Robokassa provider is not configured", 503);

        if (userId == Guid.Empty)
            return Result.Failure<PaymentCheckout>("Invalid user ID", 400);

        if (amount <= Money.Zero)
            return Result.Failure<PaymentCheckout>("Amount must be positive", 400);

        var outSum = FormatAmount(amount);
        var customParameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Shp_operationId"] = operationId.ToString(),
            ["Shp_userId"] = userId.ToString()
        };
        var signature = BuildSignature(
            $"{_options.MerchantLogin}:{outSum}:{invoiceId}:{_options.Password1}",
            customParameters);

        var query = new SortedDictionary<string, string>
        {
            ["MerchantLogin"] = _options.MerchantLogin,
            ["OutSum"] = outSum,
            ["InvId"] = invoiceId.ToString(CultureInfo.InvariantCulture),
            ["Description"] = $"AuHub wallet top-up {operationId}",
            ["SignatureValue"] = signature,
            ["Culture"] = _options.Culture,
            ["IsTest"] = _options.IsTest ? "1" : "0"
        };

        foreach (var parameter in customParameters)
            query[parameter.Key] = parameter.Value;

        var paymentUrl = $"{_options.PaymentUrl}?{BuildQueryString(query)}";
        return Result.Success(new PaymentCheckout(ProviderName, operationId, invoiceId, paymentUrl, _options.IsTest));
    }

    public Result<PaymentCallbackConfirmation> ConfirmTopUpCallback(IReadOnlyDictionary<string, string> parameters)
    {
        if (!_options.IsConfigured)
            return Result.Failure<PaymentCallbackConfirmation>("Robokassa provider is not configured", 503);

        if (!TryGet(parameters, "OutSum", out var outSum) ||
            !TryGet(parameters, "InvId", out var invoiceIdValue) ||
            !TryGet(parameters, "SignatureValue", out var signature) ||
            !TryGet(parameters, "Shp_operationId", out var operationIdValue) ||
            !TryGet(parameters, "Shp_userId", out var userIdValue))
        {
            return Result.Failure<PaymentCallbackConfirmation>("Robokassa callback is missing required fields", 400);
        }

        var customParameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["Shp_operationId"] = operationIdValue,
            ["Shp_userId"] = userIdValue
        };
        var expectedSignature = BuildSignature($"{outSum}:{invoiceIdValue}:{_options.Password2}", customParameters);
        if (!FixedTimeEquals(expectedSignature, signature))
            return Result.Failure<PaymentCallbackConfirmation>("Invalid Robokassa signature", 400);

        if (!Guid.TryParse(operationIdValue, out var operationId) ||
            !Guid.TryParse(userIdValue, out var userId) ||
            !long.TryParse(invoiceIdValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var invoiceId) ||
            !decimal.TryParse(outSum, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return Result.Failure<PaymentCallbackConfirmation>("Invalid Robokassa callback values", 400);
        }

        return Result.Success(new PaymentCallbackConfirmation(
            ProviderName,
            operationId,
            userId,
            Money.FromDecimal(amount),
            invoiceId));
    }

    private static bool TryGet(IReadOnlyDictionary<string, string> parameters, string key, out string value)
    {
        if (parameters.TryGetValue(key, out value!))
            return true;

        var match = parameters.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
        value = match.Value;
        return match.Key != null;
    }

    private static string FormatAmount(Money amount)
    {
        return amount.Amount.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string BuildSignature(string baseString, SortedDictionary<string, string> customParameters)
    {
        var signatureSource = new StringBuilder(baseString);
        foreach (var parameter in customParameters)
            signatureSource.Append(':').Append(parameter.Key).Append('=').Append(parameter.Value);

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(signatureSource.ToString()));
        return Convert.ToHexString(hash).ToUpperInvariant();
    }

    private static bool FixedTimeEquals(string expectedSignature, string actualSignature)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature.ToUpperInvariant());
        var actualBytes = Encoding.UTF8.GetBytes(actualSignature.ToUpperInvariant());
        return expectedBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static string BuildQueryString(SortedDictionary<string, string> parameters)
    {
        return string.Join("&", parameters.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
    }
}

public record RobokassaOptions
{
    public string MerchantLogin { get; init; } = string.Empty;
    public string Password1 { get; init; } = string.Empty;
    public string Password2 { get; init; } = string.Empty;
    public string PaymentUrl { get; init; } = "https://auth.robokassa.ru/Merchant/Index.aspx";
    public string Culture { get; init; } = "ru";
    public bool IsTest { get; init; } = true;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(MerchantLogin) &&
        !string.IsNullOrWhiteSpace(Password1) &&
        !string.IsNullOrWhiteSpace(Password2);
}
