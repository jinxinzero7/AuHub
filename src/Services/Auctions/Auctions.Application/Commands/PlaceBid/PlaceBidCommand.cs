namespace Auctions.Application.Commands.PlaceBid;

public record PlaceBidCommand
{
    public Guid LotId { get; init; }
    public Guid BidderId { get; init; }
    public string BidderName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
