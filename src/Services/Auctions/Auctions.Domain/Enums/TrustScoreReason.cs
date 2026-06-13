namespace Auctions.Domain.Enums;

public enum TrustScoreReason
{
    SuccessfulSale = 0,
    SellerLostDispute = 1,
    BuyerLostDispute = 2,
    DeliveryRequestExpired = 3
}
