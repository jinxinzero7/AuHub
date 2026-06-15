using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Auctions.Application.Services;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Commands.CompleteLot;

public class CompleteLotCommandHandler
{
    private readonly ILotRepository _lotRepository;
    private readonly AuctionSettlementService _settlementService;

    public CompleteLotCommandHandler(
        ILotRepository lotRepository,
        AuctionSettlementService settlementService)
    {
        _lotRepository = lotRepository;
        _settlementService = settlementService;
    }

    public async Task<Result<CompleteLotResponse>> HandleAsync(
        CompleteLotCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lot = await _lotRepository.GetByIdAsync(command.LotId, cancellationToken);

            if (lot == null)
            {
                return Result.Failure<CompleteLotResponse>("Lot not found", 404);
            }

            if (command.RequireSellerOwnership && lot.SellerId != command.ActorUserId)
            {
                return Result.Failure<CompleteLotResponse>("Only seller can complete this lot", 403);
            }

            if (command.RequireBid && !lot.Bids.Any())
            {
                return Result.Failure<CompleteLotResponse>("At least one bid is required to complete the lot for demo flow", 400);
            }

            lot.Complete();
            if (lot.WinnerId.HasValue)
            {
                var chargeResult = await _settlementService.ChargeWinnerAsync(lot, cancellationToken);
                if (chargeResult.IsFailure)
                {
                    lot.ClearDomainEvents();
                    return Result.Failure<CompleteLotResponse>(chargeResult.Error, chargeResult.StatusCode);
                }

                lot.OpenDeliveryRequestWindow();
            }

            await _lotRepository.SaveChangesAsync(cancellationToken);

            var response = new CompleteLotResponse
            {
                Success = true,
                LotId = lot.Id,
                FinalPrice = lot.CurrentPrice,
                Message = "Lot force-completed successfully"
            };

            return Result.Success(response);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CompleteLotResponse>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return Result.Failure<CompleteLotResponse>($"Failed to complete lot: {ex.Message}", 500);
        }
    }
}

public record CompleteLotResponse
{
    public bool Success { get; init; }
    public Guid LotId { get; init; }
    public Money FinalPrice { get; init; } = Money.Zero;
    public string Message { get; init; } = string.Empty;
}
