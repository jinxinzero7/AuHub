namespace Auctions.Domain.Enums;

public enum NotificationType
{
    NewBid = 0,
    Outbid = 1,
    WonAuction = 2,
    LotCompleted = 3,
    AuctionEndingSoon = 4,
    LotApproved = 5,
    LotRejected = 6,
    LotFrozen = 7,
    DisputeResolved = 8
}
