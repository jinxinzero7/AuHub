namespace Auctions.Application.Commands.PlaceBid;

public record PlaceBidCommand
{
    public Guid LotId { get; init; }
    public Guid BidderId { get; init; }
    public decimal Amount { get; init; }
}
