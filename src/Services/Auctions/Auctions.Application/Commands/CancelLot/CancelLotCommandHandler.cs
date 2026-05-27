using AuHub.Shared.Results;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Commands.CancelLot;

public class CancelLotCommandHandler
{
    private readonly ILotRepository _lotRepository;

    public CancelLotCommandHandler(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public async Task<Result<CancelLotResponse>> HandleAsync(
        CancelLotCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lot = await _lotRepository.GetByIdAsync(command.LotId, cancellationToken);

            if (lot == null)
            {
                return Result.Failure<CancelLotResponse>("Lot not found", 404);
            }

            if (lot.SellerId != userId)
            {
                return Result.Failure<CancelLotResponse>("You are not the owner of this lot", 403);
            }

            lot.Cancel();

            await _lotRepository.SaveChangesAsync(cancellationToken);

            var response = new CancelLotResponse
            {
                Success = true,
                LotId = lot.Id,
                Message = "Lot cancelled successfully"
            };

            return Result.Success(response);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CancelLotResponse>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return Result.Failure<CancelLotResponse>($"Failed to cancel lot: {ex.Message}", 500);
        }
    }
}

public record CancelLotResponse
{
    public bool Success { get; init; }
    public Guid LotId { get; init; }
    public string Message { get; init; } = string.Empty;
}
