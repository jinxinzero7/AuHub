namespace AuHub.Shared.Contracts;

public record BidPlacedEvent
{
    public Guid LotId { get; init; }
    public Guid BidderId { get; init; }
    public string BidderName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public Guid SellerId { get; init; }
    public string LotTitle { get; init; } = string.Empty;
}
