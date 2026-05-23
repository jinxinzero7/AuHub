namespace Auctions.Application.Queries.GetLots;

public record GetLotsQuery
{
    public bool OnlyActive { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SellerId { get; init; }
    public string? WinnerId { get; init; }
    public bool IncludeDrafts { get; init; } = false;
    public string? Search { get; init; }
}
