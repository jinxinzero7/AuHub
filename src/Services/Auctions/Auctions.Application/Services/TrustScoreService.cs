using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Services;

public class TrustScoreService
{
    public const int BaselineScore = 70;
    private const int SuccessfulSalePoints = 5;
    private const int SellerLostDisputePoints = -15;
    private const int BuyerLostDisputePoints = -8;
    private const int DeliveryRequestExpiredPoints = -5;

    private readonly ITrustScoreEventRepository _repository;

    public TrustScoreService(ITrustScoreEventRepository repository)
    {
        _repository = repository;
    }

    public Task RecordSuccessfulSaleAsync(Lot lot, CancellationToken cancellationToken = default)
    {
        return AddEventAsync(
            lot.SellerId,
            TrustScoreSubject.Seller,
            TrustScoreReason.SuccessfulSale,
            SuccessfulSalePoints,
            lot.Id,
            cancellationToken);
    }

    public async Task RecordDisputeResolvedAsync(Lot lot, bool inFavorOfBuyer, CancellationToken cancellationToken = default)
    {
        if (inFavorOfBuyer)
        {
            await AddEventAsync(
                lot.SellerId,
                TrustScoreSubject.Seller,
                TrustScoreReason.SellerLostDispute,
                SellerLostDisputePoints,
                lot.Id,
                cancellationToken);
            return;
        }

        if (lot.WinnerId.HasValue)
        {
            await AddEventAsync(
                lot.WinnerId.Value,
                TrustScoreSubject.Buyer,
                TrustScoreReason.BuyerLostDispute,
                BuyerLostDisputePoints,
                lot.Id,
                cancellationToken);
        }
    }

    public async Task RecordDeliveryRequestExpiredAsync(Lot lot, CancellationToken cancellationToken = default)
    {
        if (!lot.WinnerId.HasValue)
            return;

        await AddEventAsync(
            lot.WinnerId.Value,
            TrustScoreSubject.Buyer,
            TrustScoreReason.DeliveryRequestExpired,
            DeliveryRequestExpiredPoints,
            lot.Id,
            cancellationToken);
    }

    public async Task<SellerTrustScoreResponse> GetSellerTrustScoreAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        var events = await _repository.GetByUserIdAsync(sellerId, TrustScoreSubject.Seller, cancellationToken);
        var score = ClampScore(BaselineScore + events.Sum(trustEvent => trustEvent.Points));

        return new SellerTrustScoreResponse
        {
            SellerId = sellerId,
            Score = score,
            Badge = GetBadge(score),
            EventsCount = events.Count,
            SuccessfulSales = events.Count(trustEvent => trustEvent.Reason == TrustScoreReason.SuccessfulSale),
            SellerLostDisputes = events.Count(trustEvent => trustEvent.Reason == TrustScoreReason.SellerLostDispute)
        };
    }

    private async Task AddEventAsync(
        Guid userId,
        TrustScoreSubject subject,
        TrustScoreReason reason,
        int points,
        Guid referenceId,
        CancellationToken cancellationToken)
    {
        var exists = await _repository.ExistsAsync(userId, subject, reason, referenceId, cancellationToken);
        if (exists)
            return;

        await _repository.AddAsync(
            TrustScoreEvent.Create(userId, subject, reason, points, "Lot", referenceId),
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static int ClampScore(int score)
    {
        return Math.Clamp(score, 0, 100);
    }

    private static string GetBadge(int score)
    {
        return score switch
        {
            >= 85 => "Excellent",
            >= 70 => "Reliable",
            >= 50 => "Watch",
            _ => "Risk"
        };
    }
}

public record SellerTrustScoreResponse
{
    public Guid SellerId { get; init; }
    public int Score { get; init; }
    public string Badge { get; init; } = string.Empty;
    public int EventsCount { get; init; }
    public int SuccessfulSales { get; init; }
    public int SellerLostDisputes { get; init; }
}
