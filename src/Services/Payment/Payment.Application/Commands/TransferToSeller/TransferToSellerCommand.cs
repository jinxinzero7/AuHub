using AuHub.Shared.ValueObjects;

namespace Payment.Application.Commands.TransferToSeller;

public record TransferToSellerCommand
{
    public static readonly Guid PlatformWalletUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid SellerId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Money ServiceFee { get; init; } = Money.Zero;
    public Guid ReferenceId { get; init; }
}
