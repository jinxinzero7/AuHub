namespace Auctions.Domain.Entities;

public class Lot
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal StartingPrice { get; private set; }
    public decimal CurrentPrice { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public Guid SellerId { get; private set; }
    public LotStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    private readonly List<Bid> _bids = new();
    public IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();

    // Приватный конструктор для EF Core
    private Lot() { }

    // Фабричный метод создания
    public static Lot Create(
        string title,
        string description,
        decimal startingPrice,
        DateTime startTime,
        DateTime endTime,
        Guid sellerId)
    {
        return new Lot
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartingPrice = startingPrice,
            CurrentPrice = startingPrice,
            StartTime = startTime,
            EndTime = endTime,
            SellerId = sellerId,
            Status = LotStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Domain методы
    public void Publish()
    {
        if (Status != LotStatus.Draft)
            throw new InvalidOperationException("Only draft lots can be published");

        Status = LotStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PlaceBid(Guid bidderId, decimal amount)
    {
        if (Status != LotStatus.Active)
            throw new InvalidOperationException("Lot is not active");

        if (amount <= CurrentPrice)
            throw new InvalidOperationException("Bid amount must be higher than current price");

        if (DateTime.UtcNow > EndTime)
            throw new InvalidOperationException("Auction has ended");

        var bid = Bid.Create(Id, bidderId, amount);
        _bids.Add(bid);
        CurrentPrice = amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != LotStatus.Active)
            throw new InvalidOperationException("Only active lots can be completed");

        Status = LotStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == LotStatus.Completed)
            throw new InvalidOperationException("Cannot cancel completed lot");

        Status = LotStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
