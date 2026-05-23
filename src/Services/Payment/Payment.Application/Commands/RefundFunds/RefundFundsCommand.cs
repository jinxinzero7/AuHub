using AuHub.Shared.ValueObjects;

namespace Payment.Application.Commands.RefundFunds;

public record RefundFundsCommand
{
    public Guid UserId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Guid ReferenceId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
