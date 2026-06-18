using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Auctions.Application.Mappings;
using Auctions.Application.Services;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Queries.GetBidsByLot;

public class GetBidsByLotQueryHandler
{
    private readonly IBidRepository _bidRepository;
    private readonly ILotRepository _lotRepository;

    public GetBidsByLotQueryHandler(IBidRepository bidRepository, ILotRepository lotRepository)
    {
        _bidRepository = bidRepository;
        _lotRepository = lotRepository;
    }

    public async Task<Result<List<BidResponse>>> HandleAsync(
        GetBidsByLotQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lot = await _lotRepository.GetByIdAsync(query.LotId, cancellationToken);
            if (lot == null || !LotVisibilityPolicy.CanViewDetails(lot, query.RequesterUserId, query.RequesterIsAdmin))
                return Result.Failure<List<BidResponse>>("Lot not found", 404);

            var bids = await _bidRepository.GetByLotIdAsync(query.LotId, cancellationToken);

            var response = bids.Select(b => b.ToResponse(query.RequesterIsAdmin)).ToList();

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
    public Guid? BidderId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public DateTime PlacedAt { get; init; }
}
