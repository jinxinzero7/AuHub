using AuHub.Shared.ValueObjects;

namespace Auctions.Application.Queries.GetAdminUserActivity;

public record AdminUserActivityResponse
{
    public Guid UserId { get; init; }
    public int CreatedLotsCount { get; init; }
    public int BidsCount { get; init; }
    public int WinsCount { get; init; }
    public int ActiveDealsCount { get; init; }
    public Dictionary<string, int> LotStatusCounts { get; init; } = [];
    public AdminUserLotsPage CreatedLots { get; init; } = new();
    public List<AdminUserBidSummary> RecentBids { get; init; } = [];
    public AdminSellerRatingSummary SellerRating { get; init; } = new();
    public AdminSellerTrustSummary SellerTrust { get; init; } = new();
    public List<AdminTrustEventSummary> RecentTrustEvents { get; init; } = [];
}

public record AdminUserLotsPage
{
    public List<AdminUserLotSummary> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public record AdminUserLotSummary
{
    public Guid LotId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Money CurrentPrice { get; init; } = Money.Zero;
    public int BidsCount { get; init; }
    public DateTime? EndTime { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record AdminUserBidSummary
{
    public Guid BidId { get; init; }
    public Guid LotId { get; init; }
    public string LotTitle { get; init; } = string.Empty;
    public string LotStatus { get; init; } = string.Empty;
    public Money Amount { get; init; } = Money.Zero;
    public DateTime PlacedAt { get; init; }
}

public record AdminSellerRatingSummary
{
    public int ReviewsCount { get; init; }
    public double AverageRating { get; init; }
}

public record AdminSellerTrustSummary
{
    public int Score { get; init; }
    public string Badge { get; init; } = string.Empty;
    public int EventsCount { get; init; }
}

public record AdminTrustEventSummary
{
    public Guid EventId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public int Points { get; init; }
    public string ReferenceType { get; init; } = string.Empty;
    public Guid ReferenceId { get; init; }
    public DateTime CreatedAt { get; init; }
}
