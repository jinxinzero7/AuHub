using Auctions.Domain.Common;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Queries.GetLots;

public class GetLotsQueryHandler
{
    private readonly ILotRepository _lotRepository;

    public GetLotsQueryHandler(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public async Task<Result<List<LotResponse>>> HandleAsync(
        GetLotsQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lots = query.OnlyActive
                ? await _lotRepository.GetActiveLotsAsync(cancellationToken)
                : await _lotRepository.GetAllAsync(cancellationToken);

            var response = lots.Select(lot => new LotResponse
            {
                Id = lot.Id,
                Title = lot.Title,
                Description = lot.Description,
                StartingPrice = lot.StartingPrice,
                CurrentPrice = lot.CurrentPrice,
                StartTime = lot.StartTime,
                EndTime = lot.EndTime,
                Status = lot.Status.ToString(),
                BidsCount = lot.Bids.Count
            }).ToList();

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<LotResponse>>($"Failed to get lots: {ex.Message}", 500);
        }
    }
}
