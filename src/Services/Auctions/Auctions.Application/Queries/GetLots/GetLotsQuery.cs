namespace Auctions.Application.Queries.GetLots;

public record GetLotsQuery
{
    public bool OnlyActive { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
