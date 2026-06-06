namespace Auctions.Domain.Entities;

public enum LotStatus
{
    Draft = 0,
    PendingModeration = 1,
    Rejected = 2,
    Active = 3,
    Frozen = 4,
    Cancelled = 5,
    Completed = 6,
    DeliveryRequestPending = 7,
    ShippingPending = 8,
    Shipped = 9,
    Delivered = 10,
    TransactionComplete = 11,
    Disputed = 12,
    DeliveryRequestExpired = 13
}
