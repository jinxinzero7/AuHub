using AuHub.Shared.Results;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Queries.GetLotById;

public class GetLotByIdQueryHandler
{
    private readonly ILotRepository _lotRepository;

    public GetLotByIdQueryHandler(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public async Task<Result<LotDetailResponse>> HandleAsync(
        GetLotByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lot = await _lotRepository.GetByIdAsync(query.LotId, cancellationToken);

            if (lot == null)
            {
                return Result.Failure<LotDetailResponse>("Lot not found", 404);
            }

            var response = new LotDetailResponse
            {
                Id = lot.Id,
                Title = lot.Title,
                Description = lot.Description,
                StartingPrice = lot.StartingPrice,
                CurrentPrice = lot.CurrentPrice,
                StartTime = lot.StartTime,
                EndTime = lot.EndTime,
                SellerId = lot.SellerId,
                Status = lot.Status.ToString(),
                CreatedAt = lot.CreatedAt,
                UpdatedAt = lot.UpdatedAt,
                BidsCount = lot.Bids.Count,
                Bids = lot.Bids.Select(b => new BidDto
                {
                    Id = b.Id,
                    BidderId = b.BidderId,
                    Amount = b.Amount,
                    PlacedAt = b.PlacedAt
                }).OrderByDescending(b => b.PlacedAt).ToList()
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            return Result.Failure<LotDetailResponse>($"Failed to get lot: {ex.Message}", 500);
        }
    }
}
