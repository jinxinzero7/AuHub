using AuHub.Shared.Results;
using Auctions.Application.Mappings;
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
            List<Auctions.Domain.Entities.Lot> lots;

            if (!string.IsNullOrEmpty(query.SellerId))
            {
                var sellerId = Guid.Parse(query.SellerId);
                lots = await _lotRepository.GetBySellerIdAsync(sellerId, query.IncludeDrafts, cancellationToken);

                if (query.OnlyActive)
                {
                    lots = lots
                        .Where(lot => lot.SellerId == sellerId &&
                                      lot.Status == Auctions.Domain.Entities.LotStatus.Active &&
                                      !lot.IsDeleted)
                        .ToList();
                }
            }
            else if (!string.IsNullOrEmpty(query.WinnerId))
            {
                var winnerId = Guid.Parse(query.WinnerId);
                lots = await _lotRepository.GetByWinnerIdAsync(winnerId, cancellationToken);
            }
            else if (query.OnlyActive)
            {
                lots = await _lotRepository.GetActiveLotsAsync(query.Search, cancellationToken);
            }
            else if (query.IncludeDrafts)
            {
                lots = await _lotRepository.GetAllAsync(cancellationToken);
            }
            else
            {
                lots = await _lotRepository.GetPublicLotsAsync(query.Search, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(query.Search) && (!string.IsNullOrEmpty(query.SellerId) || !string.IsNullOrEmpty(query.WinnerId)))
            {
                var term = query.Search.ToLower();
                lots = lots.Where(l => l.Title.ToLower().Contains(term) || l.Description.ToLower().Contains(term)).ToList();
            }

            var totalCount = lots.Count;
            var paginatedLots = lots
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(lot => lot.ToResponse(query.IncludePrivateDeliveryDetails))
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
