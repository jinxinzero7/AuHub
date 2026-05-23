using AuHub.Shared.ValueObjects;

namespace Payment.Application.Commands.ReleaseFunds;

public record ReleaseFundsCommand
{
    public Guid UserId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Guid ReferenceId { get; init; }
}
