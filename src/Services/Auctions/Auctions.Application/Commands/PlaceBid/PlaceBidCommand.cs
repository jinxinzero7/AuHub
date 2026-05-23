using AuHub.Shared.ValueObjects;

namespace Auctions.Application.Commands.PlaceBid;

public record PlaceBidCommand
{
    public Guid LotId { get; init; }
    public Guid BidderId { get; init; }
    public string BidderName { get; init; } = string.Empty;
    public Money Amount { get; init; } = Money.Zero;
    public Guid? IdempotencyKey { get; init; }
}
