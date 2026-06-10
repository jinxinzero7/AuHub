using AuHub.Shared.ValueObjects;
using Payment.Domain.Enums;

namespace Payment.Application.Queries.GetTransactionHistory;

public record GetTransactionHistoryQuery
{
    public Guid UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record TransactionDto
{
    public Guid Id { get; init; }
    public TransactionType Type { get; init; }
    public string Effect { get; init; } = string.Empty;
    public Money Amount { get; init; } = Money.Zero;
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public Guid? ReferenceId { get; init; }
}

public record TransactionHistoryResponse
{
    public List<TransactionDto> Transactions { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}
