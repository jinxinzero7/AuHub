namespace Auctions.Application.Queries.GetLots;

public record GetLotsQuery
{
    public bool OnlyActive { get; init; } = false;
}
