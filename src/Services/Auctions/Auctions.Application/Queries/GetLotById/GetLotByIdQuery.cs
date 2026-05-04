namespace Auctions.Application.Queries.GetLotById;

public record GetLotByIdQuery
{
    public Guid LotId { get; init; }
}
