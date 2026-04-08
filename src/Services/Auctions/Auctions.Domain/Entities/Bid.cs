namespace Auctions.Domain.Entities;

public class Bid
{
    public Guid Id { get; private set; }
    public Guid LotId { get; private set; }
    public Guid BidderId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PlacedAt { get; private set; }

    // Navigation
    public Lot Lot { get; private set; } = null!;

    // Приватный конструктор для EF Core
    private Bid() { }

    // Фабричный метод создания
    public static Bid Create(Guid lotId, Guid bidderId, decimal amount)
    {
        return new Bid
        {
            Id = Guid.NewGuid(),
            LotId = lotId,
            BidderId = bidderId,
            Amount = amount,
            PlacedAt = DateTime.UtcNow
        };
    }
}
