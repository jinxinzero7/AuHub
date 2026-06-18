using AuHub.Shared.Results;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Queries.GetAdminUserActivity;

public class GetAdminUserActivityQueryHandler
{
    private const int RecentItemsLimit = 20;

    private readonly ILotRepository _lotRepository;
    private readonly IBidRepository _bidRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly ITrustScoreEventRepository _trustRepository;

    public GetAdminUserActivityQueryHandler(
        ILotRepository lotRepository,
        IBidRepository bidRepository,
        IReviewRepository reviewRepository,
        ITrustScoreEventRepository trustRepository)
    {
        _lotRepository = lotRepository;
        _bidRepository = bidRepository;
        _reviewRepository = reviewRepository;
        _trustRepository = trustRepository;
    }

    public async Task<Result<AdminUserActivityResponse>> HandleAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var createdLots = await _lotRepository.GetBySellerIdAsync(userId, true, cancellationToken);
        var wonLots = await _lotRepository.GetByWinnerIdAsync(userId, cancellationToken);
        var bids = await _bidRepository.GetByBidderIdAsync(userId, cancellationToken);
        var reviews = await _reviewRepository.GetBySellerIdAsync(userId, cancellationToken);
        var sellerTrustEvents = await _trustRepository.GetByUserIdAsync(userId, TrustScoreSubject.Seller, cancellationToken);
        var buyerTrustEvents = await _trustRepository.GetByUserIdAsync(userId, TrustScoreSubject.Buyer, cancellationToken);

        var totalCount = createdLots.Count;
        var pagedLots = createdLots
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(lot => lot.ToAdminUserSummary())
            .ToList();
        var allTrustEvents = sellerTrustEvents
            .Concat(buyerTrustEvents)
            .OrderByDescending(trustEvent => trustEvent.CreatedAt)
            .ToList();
        var sellerScore = TrustScoreService.CalculateScore(sellerTrustEvents);

        return Result.Success(new AdminUserActivityResponse
        {
            UserId = userId,
            CreatedLotsCount = createdLots.Count,
            BidsCount = bids.Count,
            WinsCount = wonLots.Count,
            ActiveDealsCount = createdLots.Concat(wonLots)
                .Where(lot => IsDealInProgress(lot.Status))
                .Select(lot => lot.Id)
                .Distinct()
                .Count(),
            LotStatusCounts = createdLots
                .GroupBy(lot => lot.Status.ToString())
                .ToDictionary(group => group.Key, group => group.Count()),
            CreatedLots = new AdminUserLotsPage
            {
                Items = pagedLots,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            },
            RecentBids = bids.OrderByDescending(bid => bid.PlacedAt)
                .Take(RecentItemsLimit)
                .Select(bid => bid.ToAdminUserSummary())
                .ToList(),
            SellerRating = new AdminSellerRatingSummary
            {
                ReviewsCount = reviews.Count,
                AverageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(review => review.Rating), 2)
            },
            SellerTrust = new AdminSellerTrustSummary
            {
                Score = sellerScore,
                Badge = TrustScoreService.GetBadge(sellerScore),
                EventsCount = sellerTrustEvents.Count
            },
            RecentTrustEvents = allTrustEvents.Take(RecentItemsLimit)
                .Select(trustEvent => trustEvent.ToAdminUserSummary())
                .ToList()
        });
    }

    private static bool IsDealInProgress(LotStatus status) => status is
        LotStatus.DeliveryRequestPending or LotStatus.ShippingPending or LotStatus.Shipped or LotStatus.Disputed;

}
