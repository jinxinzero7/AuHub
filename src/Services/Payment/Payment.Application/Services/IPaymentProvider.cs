using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;

namespace Payment.Application.Services;

public interface IPaymentProvider
{
    Task<Result<PaymentProviderConfirmation>> ConfirmTopUpAsync(
        Guid userId,
        Money amount,
        CancellationToken cancellationToken = default);
}

public record PaymentProviderConfirmation(
    string Provider,
    string OperationId);
