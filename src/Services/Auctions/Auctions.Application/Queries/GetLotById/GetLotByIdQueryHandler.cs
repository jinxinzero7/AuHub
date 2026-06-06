using AuHub.Shared.Results;
using Auctions.Application.Mappings;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Queries.GetLotById;

public class GetLotByIdQueryHandler
{
    private readonly ILotRepository _lotRepository;

    public GetLotByIdQueryHandler(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public async Task<Result<LotDetailResponse>> HandleAsync(
        GetLotByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lot = await _lotRepository.GetByIdAsync(query.LotId, cancellationToken);

            if (lot == null)
            {
                return Result.Failure<LotDetailResponse>("Lot not found", 404);
            }

            var response = lot.ToDetailResponse();

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            return Result.Failure<LotDetailResponse>($"Failed to get lot: {ex.Message}", 500);
        }
    }
}
