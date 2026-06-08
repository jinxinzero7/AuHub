using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Payment.Application.Repositories;
using Payment.Domain.Enums;

namespace Payment.Application.Services;

public static class PaymentOperationIdempotency
{
    public static async Task<Result<bool>?> CheckAsync(
        ITransactionRepository transactionRepository,
        Guid userId,
        TransactionType type,
        Money amount,
        Guid referenceId,
        CancellationToken cancellationToken)
    {
        var existing = await transactionRepository.GetByUserIdTypeAndReferenceIdAsync(
            userId,
            type,
            referenceId,
            cancellationToken);

        if (existing == null)
        {
            return null;
        }

        if (existing.Amount != amount)
        {
            return Result.Failure<bool>("Payment operation already exists with a different amount", 409);
        }

        return Result.Success(true);
    }
}
