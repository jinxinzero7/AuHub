using AuHub.Shared.Results;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Queries.GetLots;

public class GetLotsQueryHandler
{
    private readonly ILotRepository _lotRepository;

    public GetLotsQueryHandler(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public async Task<Result<PaginatedLotsResponse>> HandleAsync(
        GetLotsQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lots = query.OnlyActive
                ? await _lotRepository.GetActiveLotsAsync(cancellationToken)
                : await _lotRepository.GetAllAsync(cancellationToken);

            // Применяем пагинацию
            var totalCount = lots.Count;
            var paginatedLots = lots
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(lot => new LotResponse
                {
                    Id = lot.Id,
                    Title = lot.Title,
                    Description = lot.Description,
                    StartingPrice = lot.StartingPrice,
                    CurrentPrice = lot.CurrentPrice,
                    StartTime = lot.StartTime,
                    EndTime = lot.EndTime,
                    Status = lot.Status.ToString(),
                    BidsCount = lot.Bids.Count
                })
                .ToList();

            var response = new PaginatedLotsResponse
            {
                Lots = paginatedLots,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            return Result.Failure<PaginatedLotsResponse>($"Failed to get lots: {ex.Message}", 500);
        }
    }
}

public record PaginatedLotsResponse
{
    public List<LotResponse> Lots { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}
