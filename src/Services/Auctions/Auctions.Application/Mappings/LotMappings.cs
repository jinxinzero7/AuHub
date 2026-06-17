using Auctions.Application.Queries.GetBidsByLot;
using Auctions.Application.Queries.GetLotById;
using Auctions.Application.Queries.GetLots;
using Auctions.Domain.Entities;

namespace Auctions.Application.Mappings;

public static class LotMappings
{
    public static LotResponse ToResponse(this Lot lot, bool includePrivateDeliveryDetails = false)
    {
        return new LotResponse
        {
            Id = lot.Id,
            Title = lot.Title,
            Description = lot.Description,
            StartingPrice = lot.StartingPrice,
            CurrentPrice = lot.CurrentPrice,
            DurationHours = (int)lot.Duration.TotalHours,
            StartTime = lot.StartTime,
            EndTime = lot.EndTime,
            Status = lot.Status.ToString(),
            SellerId = lot.SellerId,
            WinnerId = lot.WinnerId,
            BidsCount = lot.Bids.Count,
            CoverImageUrl = lot.CoverImageUrl,
            TrackingNumber = includePrivateDeliveryDetails ? lot.TrackingNumber : null,
            DeliveryAddress = includePrivateDeliveryDetails ? lot.DeliveryAddress : null,
            DeliveryRecipientName = includePrivateDeliveryDetails ? lot.DeliveryRecipientName : null,
            DeliveryRecipientPhone = includePrivateDeliveryDetails ? lot.DeliveryRecipientPhone : null,
            SelectedDeliveryProvider = includePrivateDeliveryDetails ? lot.SelectedDeliveryProvider?.ToString() : null,
            DeliveryRequestedAt = includePrivateDeliveryDetails ? lot.DeliveryRequestedAt : null,
            DeliveryRequestDeadlineAt = includePrivateDeliveryDetails ? lot.DeliveryRequestDeadlineAt : null,
            SupportedDeliveryProviders = lot.SupportedDeliveryProviders.Select(provider => provider.ToString()).ToList(),
            AdminComment = lot.AdminComment,
            CreatedAt = lot.CreatedAt
        };
    }

    public static LotDetailResponse ToDetailResponse(this Lot lot, Guid? requesterUserId = null, bool requesterIsAdmin = false)
    {
        var includePrivateDeliveryDetails = CanViewPrivateDeliveryDetails(lot, requesterUserId, requesterIsAdmin);

        return new LotDetailResponse
        {
            Id = lot.Id,
            Title = lot.Title,
            Description = lot.Description,
            StartingPrice = lot.StartingPrice,
            CurrentPrice = lot.CurrentPrice,
            DurationHours = (int)lot.Duration.TotalHours,
            StartTime = lot.StartTime,
            EndTime = lot.EndTime,
            SellerId = lot.SellerId,
            WinnerId = lot.WinnerId,
            Status = lot.Status.ToString(),
            CreatedAt = lot.CreatedAt,
            UpdatedAt = lot.UpdatedAt,
            BidsCount = lot.Bids.Count,
            TrackingNumber = includePrivateDeliveryDetails ? lot.TrackingNumber : null,
            DeliveryAddress = includePrivateDeliveryDetails ? lot.DeliveryAddress : null,
            DeliveryRecipientName = includePrivateDeliveryDetails ? lot.DeliveryRecipientName : null,
            DeliveryRecipientPhone = includePrivateDeliveryDetails ? lot.DeliveryRecipientPhone : null,
            SelectedDeliveryProvider = includePrivateDeliveryDetails ? lot.SelectedDeliveryProvider?.ToString() : null,
            DeliveryRequestedAt = includePrivateDeliveryDetails ? lot.DeliveryRequestedAt : null,
            DeliveryRequestDeadlineAt = includePrivateDeliveryDetails ? lot.DeliveryRequestDeadlineAt : null,
            SupportedDeliveryProviders = lot.SupportedDeliveryProviders.Select(provider => provider.ToString()).ToList(),
            AdminComment = requesterIsAdmin || requesterUserId == lot.SellerId ? lot.AdminComment : null,
            Bids = lot.Bids.Select(b => b.ToDetailDto()).OrderByDescending(b => b.PlacedAt).ToList()
        };
    }

    private static bool CanViewPrivateDeliveryDetails(Lot lot, Guid? requesterUserId, bool requesterIsAdmin)
    {
        return requesterIsAdmin ||
               requesterUserId == lot.SellerId ||
               requesterUserId == lot.WinnerId;
    }

    public static BidDto ToDetailDto(this Bid bid)
    {
        return new BidDto
        {
            Id = bid.Id,
            BidderId = bid.BidderId,
            Amount = bid.Amount,
            PlacedAt = bid.PlacedAt
        };
    }

    public static BidResponse ToResponse(this Bid bid)
    {
        return new BidResponse
        {
            Id = bid.Id,
            BidderId = bid.BidderId,
            Amount = bid.Amount,
            PlacedAt = bid.PlacedAt
        };
    }
}
