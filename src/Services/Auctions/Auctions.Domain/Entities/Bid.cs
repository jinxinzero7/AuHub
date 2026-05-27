using AuHub.Shared.ValueObjects;

namespace Auctions.Domain.Entities;

public class Bid
{
    public Guid Id { get; private set; }
    public Guid LotId { get; private set; }
    public Guid BidderId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero;
    public DateTime PlacedAt { get; private set; }
    public Guid? IdempotencyKey { get; private set; }

    public Lot Lot { get; private set; } = null!;

    private Bid() { }

    public static Bid Create(Guid lotId, Guid bidderId, Money amount, Guid? idempotencyKey = null)
    {
        return new Bid
        {
            Id = Guid.NewGuid(),
            LotId = lotId,
            BidderId = bidderId,
            Amount = amount,
            PlacedAt = DateTime.UtcNow,
            IdempotencyKey = idempotencyKey
        };
    }
}
