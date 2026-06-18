using AuHub.Shared.ValueObjects;

namespace Auctions.Application.Queries.GetLotById;

public class LotDetailResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Money StartingPrice { get; set; } = Money.Zero;
    public Money CurrentPrice { get; set; } = Money.Zero;
    public int DurationHours { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public Guid SellerId { get; set; }
    public Guid? WinnerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int BidsCount { get; set; }
    public string? TrackingNumber { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryRecipientName { get; set; }
    public string? DeliveryRecipientPhone { get; set; }
    public string? SelectedDeliveryProvider { get; set; }
    public DateTime? DeliveryRequestedAt { get; set; }
    public DateTime? DeliveryRequestDeadlineAt { get; set; }
    public List<string> SupportedDeliveryProviders { get; set; } = new();
    public string? AdminComment { get; set; }
    public List<BidDto> Bids { get; set; } = new();
}

public class BidDto
{
    public Guid Id { get; set; }
    public Guid? BidderId { get; set; }
    public Money Amount { get; set; } = Money.Zero;
    public DateTime PlacedAt { get; set; }
}
