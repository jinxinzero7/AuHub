namespace Auctions.Application.Queries.GetBidsByLot;

public record GetBidsByLotQuery
{
    public Guid LotId { get; init; }
}
