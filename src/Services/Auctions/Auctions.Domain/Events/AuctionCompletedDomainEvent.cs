namespace Auctions.Domain.Events;

public record AuctionCompletedDomainEvent : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public Guid LotId { get; init; }
    public string LotTitle { get; init; } = string.Empty;
    public Guid? WinnerId { get; init; }
    public string? WinnerName { get; init; }
    public decimal FinalPrice { get; init; }
    public Guid SellerId { get; init; }
}
