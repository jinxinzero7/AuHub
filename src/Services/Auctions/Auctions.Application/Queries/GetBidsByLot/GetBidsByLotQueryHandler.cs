using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Queries.GetBidsByLot;

public class GetBidsByLotQueryHandler
{
    private readonly IBidRepository _bidRepository;

    public GetBidsByLotQueryHandler(IBidRepository bidRepository)
    {
        _bidRepository = bidRepository;
    }

    public async Task<Result<List<BidResponse>>> HandleAsync(
        GetBidsByLotQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bids = await _bidRepository.GetByLotIdAsync(query.LotId, cancellationToken);

            var response = bids.Select(b => new BidResponse
            {
                Id = b.Id,
                BidderId = b.BidderId,
                Amount = b.Amount,
                PlacedAt = b.PlacedAt
            }).ToList();

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<BidResponse>>($"Failed to get bids: {ex.Message}", 500);
        }
    }
}

public record BidResponse
{
    public Guid Id { get; init; }
    public Guid BidderId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public DateTime PlacedAt { get; init; }
}
