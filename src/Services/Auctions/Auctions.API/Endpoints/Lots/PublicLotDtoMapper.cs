using Auctions.Application.Queries.GetLots;

namespace Auctions.API.Endpoints.Lots;

internal static class PublicLotDtoMapper
{
    public static List<LotDto> Map(
        IEnumerable<LotResponse> lots,
        string storageUrl)
    {
        var trimmedStorage = storageUrl.TrimEnd('/');

        return lots.Select(lot => new LotDto
        {
            Id = lot.Id,
            Title = lot.Title,
            Description = lot.Description,
            StartingPrice = lot.StartingPrice,
            CurrentPrice = lot.CurrentPrice,
            DurationHours = lot.DurationHours,
            StartTime = lot.StartTime,
            EndTime = lot.EndTime,
            Status = lot.Status,
            SellerId = lot.SellerId,
            BidsCount = lot.BidsCount,
            CoverImageUrl = string.IsNullOrEmpty(lot.CoverImageUrl)
                ? null
                : $"{trimmedStorage}/{lot.CoverImageUrl}",
            SupportedDeliveryProviders = lot.SupportedDeliveryProviders
        }).ToList();
    }
}
