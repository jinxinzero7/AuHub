namespace AuHub.Shared.Contracts;

public record AuctionCompletedEvent
{
    public Guid LotId { get; init; }
    public string LotTitle { get; init; } = string.Empty;
    public Guid? WinnerId { get; init; }
    public string? WinnerName { get; init; }
    public decimal FinalPrice { get; init; }
    public Guid SellerId { get; init; }
}
