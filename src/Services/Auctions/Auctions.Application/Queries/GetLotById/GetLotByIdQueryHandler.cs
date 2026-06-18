using AuHub.Shared.Results;
using Auctions.Application.Mappings;
using Auctions.Domain.Entities;
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

            if (lot == null || !CanView(lot, query))
            {
                return Result.Failure<LotDetailResponse>("Lot not found", 404);
            }

            var response = lot.ToDetailResponse(query.RequesterUserId, query.RequesterIsAdmin);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            return Result.Failure<LotDetailResponse>($"Failed to get lot: {ex.Message}", 500);
        }
    }

    private static bool CanView(Lot lot, GetLotByIdQuery query)
    {
        if (query.RequesterIsAdmin)
            return true;

        if (lot.IsDeleted)
            return false;

        if (query.RequesterUserId.HasValue &&
            (query.RequesterUserId.Value == lot.SellerId || query.RequesterUserId.Value == lot.WinnerId))
            return true;

        return lot.Status is LotStatus.Active or LotStatus.Completed or LotStatus.CompletedNoWinner;
    }
}
