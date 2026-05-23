using AuHub.Shared.ValueObjects;

namespace Payment.Application.Commands.TransferToSeller;

public record TransferToSellerCommand
{
    public Guid SellerId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Guid ReferenceId { get; init; }
}
