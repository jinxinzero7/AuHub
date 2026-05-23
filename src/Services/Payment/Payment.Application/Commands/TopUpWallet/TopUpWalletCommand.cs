using AuHub.Shared.ValueObjects;

namespace Payment.Application.Commands.TopUpWallet;

public record TopUpWalletCommand
{
    public Guid UserId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
}
