namespace Auctions.Application.Queries.GetBidsByLot;

public record GetBidsByLotQuery
{
    public Guid LotId { get; init; }
    public Guid? RequesterUserId { get; init; }
    public bool RequesterIsAdmin { get; init; }
}
