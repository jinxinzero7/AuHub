using Auctions.Domain.Entities;

namespace Auctions.Application.Queries.GetAdminUserActivity;

public static class AdminUserActivityMappings
{
    public static AdminUserLotSummary ToAdminUserSummary(this Lot lot) => new()
    {
        LotId = lot.Id,
        Title = lot.Title,
        Status = lot.Status.ToString(),
        CurrentPrice = lot.CurrentPrice,
        BidsCount = lot.Bids.Count,
        EndTime = lot.EndTime,
        CreatedAt = lot.CreatedAt
    };

    public static AdminUserBidSummary ToAdminUserSummary(this Bid bid) => new()
    {
        BidId = bid.Id,
        LotId = bid.LotId,
        LotTitle = bid.Lot?.Title ?? string.Empty,
        LotStatus = bid.Lot?.Status.ToString() ?? string.Empty,
        Amount = bid.Amount,
        PlacedAt = bid.PlacedAt
    };

    public static AdminTrustEventSummary ToAdminUserSummary(this TrustScoreEvent trustEvent) => new()
    {
        EventId = trustEvent.Id,
        Subject = trustEvent.Subject.ToString(),
        Reason = trustEvent.Reason.ToString(),
        Points = trustEvent.Points,
        ReferenceType = trustEvent.ReferenceType,
        ReferenceId = trustEvent.ReferenceId,
        CreatedAt = trustEvent.CreatedAt
    };
}
