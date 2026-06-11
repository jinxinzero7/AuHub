namespace Auctions.Domain.Entities;

public class Review
{
    public Guid Id { get; private set; }
    public Guid LotId { get; private set; }
    public Guid SellerId { get; private set; }
    public Guid BuyerId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Lot Lot { get; private set; } = null!;

    private Review() { }

    public static Review Create(Guid lotId, Guid sellerId, Guid buyerId, int rating, string? comment)
    {
        if (rating is < 1 or > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5");

        var normalizedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (normalizedComment?.Length > 1000)
            throw new InvalidOperationException("Review comment is too long");

        return new Review
        {
            Id = Guid.NewGuid(),
            LotId = lotId,
            SellerId = sellerId,
            BuyerId = buyerId,
            Rating = rating,
            Comment = normalizedComment,
            CreatedAt = DateTime.UtcNow
        };
    }
}
