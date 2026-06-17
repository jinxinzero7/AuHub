namespace Auctions.Application.Queries.GetLotById;

public record GetLotByIdQuery
{
    public Guid LotId { get; init; }
    public Guid? RequesterUserId { get; init; }
    public bool RequesterIsAdmin { get; init; }
}
