using Auctions.Domain.Interfaces;
using AuHub.Shared.Results;

namespace Auctions.Application.Commands.UpdateLot;

public class UpdateLotCommandHandler
{
    private readonly ILotRepository _lotRepository;

    public UpdateLotCommandHandler(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public async Task<Result<UpdateLotResponse>> HandleAsync(
        UpdateLotCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lot = await _lotRepository.GetByIdAsync(command.LotId, cancellationToken);
            if (lot == null)
                return Result.Failure<UpdateLotResponse>("Lot not found", 404);

            if (lot.SellerId != userId)
                return Result.Failure<UpdateLotResponse>("You are not the owner of this lot", 403);

            lot.UpdateDraft(
                command.Title,
                command.Description,
                command.StartingPrice,
                TimeSpan.FromHours(command.DurationHours),
                command.SupportedDeliveryProviders);

            if (command.SubmitForModeration)
            {
                lot.SubmitForModeration();
            }

            await _lotRepository.SaveChangesAsync(cancellationToken);

            return Result.Success(new UpdateLotResponse
            {
                Success = true,
                LotId = lot.Id,
                Status = lot.Status.ToString()
            });
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<UpdateLotResponse>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return Result.Failure<UpdateLotResponse>($"Failed to update lot: {ex.Message}", 500);
        }
    }
}

public record UpdateLotResponse
{
    public bool Success { get; init; }
    public Guid LotId { get; init; }
    public string Status { get; init; } = string.Empty;
}
