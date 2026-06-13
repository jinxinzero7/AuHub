using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;

namespace Payment.Application.Services;

public interface IPaymentCheckoutProvider
{
    string ProviderName { get; }

    Result<PaymentCheckout> CreateTopUpCheckout(
        Guid userId,
        Money amount,
        Guid operationId,
        long invoiceId);

    Result<PaymentCallbackConfirmation> ConfirmTopUpCallback(
        IReadOnlyDictionary<string, string> parameters);
}

public record PaymentCheckout(
    string Provider,
    Guid OperationId,
    long InvoiceId,
    string PaymentUrl,
    bool IsTest);

public record PaymentCallbackConfirmation(
    string Provider,
    Guid OperationId,
    Guid UserId,
    Money Amount,
    long InvoiceId);
