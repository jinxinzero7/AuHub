using AuHub.Shared.ValueObjects;

namespace Auctions.Application.Queries.GetLots;

public record LotResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Money StartingPrice { get; init; } = Money.Zero;
    public Money CurrentPrice { get; init; } = Money.Zero;
    public int DurationHours { get; init; }
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid SellerId { get; init; }
    public Guid? WinnerId { get; init; }
    public int BidsCount { get; init; }
    public string? CoverImageUrl { get; init; }
    public string? TrackingNumber { get; init; }
    public string? DeliveryAddress { get; init; }
    public string? AdminComment { get; init; }
}
