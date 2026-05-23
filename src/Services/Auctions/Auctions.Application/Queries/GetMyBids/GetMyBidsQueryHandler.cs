using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Queries.GetMyBids;

public class GetMyBidsQueryHandler
{
    private readonly IBidRepository _bidRepository;

    public GetMyBidsQueryHandler(IBidRepository bidRepository)
    {
        _bidRepository = bidRepository;
    }

    public async Task<Result<GetMyBidsResponse>> HandleAsync(
        GetMyBidsQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bids = await _bidRepository.GetByBidderIdAsync(query.UserId, cancellationToken);

            var grouped = bids
                .GroupBy(b => new { b.LotId, LotTitle = b.Lot.Title, LotStatus = b.Lot.Status.ToString() })
                .Select(g => new MyBidsGroup
                {
                    LotId = g.Key.LotId,
                    LotTitle = g.Key.LotTitle,
                    LotStatus = g.Key.LotStatus,
                    Bids = g.OrderByDescending(b => b.PlacedAt).Select(b => new MyBidItem
                    {
                        Id = b.Id,
                        Amount = b.Amount,
                        PlacedAt = b.PlacedAt
                    }).ToList()
                })
                .OrderByDescending(g => g.Bids.First().PlacedAt)
                .ToList();

            return Result.Success(new GetMyBidsResponse { Items = grouped });
        }
        catch (Exception ex)
        {
            return Result.Failure<GetMyBidsResponse>($"Failed to get bids: {ex.Message}", 500);
        }
    }
}

public class GetMyBidsResponse
{
    public List<MyBidsGroup> Items { get; init; } = new();
}

public class MyBidsGroup
{
    public Guid LotId { get; init; }
    public string LotTitle { get; init; } = string.Empty;
    public string LotStatus { get; init; } = string.Empty;
    public List<MyBidItem> Bids { get; init; } = new();
}

public class MyBidItem
{
    public Guid Id { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public DateTime PlacedAt { get; init; }
}
