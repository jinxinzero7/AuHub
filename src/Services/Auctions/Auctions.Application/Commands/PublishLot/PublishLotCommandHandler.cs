using AuHub.Shared.Results;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Commands.PublishLot;

public class PublishLotCommandHandler
{
    private readonly ILotRepository _lotRepository;

    public PublishLotCommandHandler(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public async Task<Result<PublishLotResponse>> HandleAsync(
        PublishLotCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lot = await _lotRepository.GetByIdAsync(command.LotId, cancellationToken);

            if (lot == null)
            {
                return Result.Failure<PublishLotResponse>("Lot not found", 404);
            }

            // Проверка владельца
            if (lot.SellerId != userId)
            {
                return Result.Failure<PublishLotResponse>("You are not the owner of this lot", 403);
            }

            // Вызываем domain метод Publish
            lot.Publish();

            // UpdateAsync не нужен - EF Core автоматически отслеживает изменения
            await _lotRepository.SaveChangesAsync(cancellationToken);

            var response = new PublishLotResponse
            {
                Success = true,
                LotId = lot.Id,
                Message = "Lot published successfully"
            };

            return Result.Success(response);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<PublishLotResponse>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return Result.Failure<PublishLotResponse>($"Failed to publish lot: {ex.Message}", 500);
        }
    }
}

public record PublishLotResponse
{
    public bool Success { get; init; }
    public Guid LotId { get; init; }
    public string Message { get; init; } = string.Empty;
}
