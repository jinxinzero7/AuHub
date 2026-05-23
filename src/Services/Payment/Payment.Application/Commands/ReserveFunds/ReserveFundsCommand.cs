using AuHub.Shared.ValueObjects;

namespace Payment.Application.Commands.ReserveFunds;

public record ReserveFundsCommand
{
    public Guid UserId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Guid ReferenceId { get; init; }
}
