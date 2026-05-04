namespace Auctions.Application.Queries.GetLotById;

public class LotDetailResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Guid SellerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int BidsCount { get; set; }
    public List<BidDto> Bids { get; set; } = new();
}

public class BidDto
{
    public Guid Id { get; set; }
    public Guid BidderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PlacedAt { get; set; }
}
