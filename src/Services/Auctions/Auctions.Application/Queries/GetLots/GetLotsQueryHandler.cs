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
            List<Auctions.Domain.Entities.Lot> lots;

            if (!string.IsNullOrEmpty(query.SellerId))
            {
                var sellerId = Guid.Parse(query.SellerId);
                lots = await _lotRepository.GetBySellerIdAsync(sellerId, query.IncludeDrafts, cancellationToken);
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
                .Select(lot => new LotResponse
                {
                    Id = lot.Id,
                    Title = lot.Title,
                    Description = lot.Description,
                    StartingPrice = lot.StartingPrice,
                    CurrentPrice = lot.CurrentPrice,
                    DurationHours = (int)lot.Duration.TotalHours,
                    StartTime = lot.StartTime,
                    EndTime = lot.EndTime,
                    Status = lot.Status.ToString(),
                    SellerId = lot.SellerId,
                    WinnerId = lot.WinnerId,
                    BidsCount = lot.Bids.Count,
                    CoverImageUrl = lot.Images.FirstOrDefault()?.FileName,
                    TrackingNumber = lot.TrackingNumber,
                    DeliveryAddress = lot.DeliveryAddress,
                    AdminComment = lot.AdminComment
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
