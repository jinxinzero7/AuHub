using AuHub.Shared.Results;
using Payment.Application.Repositories;
using Payment.Domain.Enums;

namespace Payment.Application.Queries.GetTransactionHistory;

public class GetTransactionHistoryQueryHandler
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionHistoryQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<TransactionHistoryResponse>> HandleAsync(GetTransactionHistoryQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var transactions = await _transactionRepository.GetByUserIdAsync(query.UserId, cancellationToken);

            var totalCount = transactions.Count;
            var paginated = transactions
                .OrderByDescending(t => t.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    Type = t.Type,
                    Effect = t.Type.GetWalletEffect(),
                    Amount = t.Amount,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    ReferenceId = t.ReferenceId
                })
                .ToList();

            return Result.Success(new TransactionHistoryResponse
            {
                Transactions = paginated,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<TransactionHistoryResponse>($"Failed to get transactions: {ex.Message}", 500);
        }
    }
}
