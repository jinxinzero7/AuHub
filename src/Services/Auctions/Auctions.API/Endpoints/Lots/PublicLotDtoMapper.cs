using Auctions.Application.Queries.GetLots;
using Auctions.Application.Services;

namespace Auctions.API.Endpoints.Lots;

internal static class PublicLotDtoMapper
{
    public static async Task<List<LotDto>> MapAsync(
        IEnumerable<LotResponse> lots,
        IImageStorageService storageService,
        CancellationToken cancellationToken)
    {
        var result = new List<LotDto>();

        foreach (var lot in lots)
        {
            string? coverImageUrl = null;
            if (!string.IsNullOrEmpty(lot.CoverImageUrl))
            {
                var fileName = lot.CoverImageUrl.Split('/').Last();
                coverImageUrl = await storageService.GetPresignedUrlAsync(
                    $"lots/{lot.Id}/{fileName}", 1440, cancellationToken);
            }

            result.Add(new LotDto
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
                CoverImageUrl = coverImageUrl,
                SupportedDeliveryProviders = lot.SupportedDeliveryProviders
            });
        }

        return result;
    }
}
