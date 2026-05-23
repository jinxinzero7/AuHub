using AuHub.Shared.ValueObjects;

namespace Payment.Application.Queries.GetBalance;

public record GetBalanceQuery
{
    public Guid UserId { get; init; }
}

public record BalanceResponse
{
    public Guid UserId { get; init; }
    public Money Balance { get; init; } = Money.Zero;
    public Money FrozenBalance { get; init; } = Money.Zero;
}
