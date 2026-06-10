using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Payment.Application.Services;

namespace Payment.Infrastructure.Services;

public class DemoPaymentProvider : IPaymentProvider
{
    public Task<Result<PaymentProviderConfirmation>> ConfirmTopUpAsync(
        Guid userId,
        Money amount,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Task.FromResult(Result.Failure<PaymentProviderConfirmation>("Invalid user ID", 400));

        if (amount <= Money.Zero)
            return Task.FromResult(Result.Failure<PaymentProviderConfirmation>("Amount must be positive", 400));

        return Task.FromResult(Result.Success(new PaymentProviderConfirmation(
            "DemoWallet",
            Guid.NewGuid().ToString("N"))));
    }
}
