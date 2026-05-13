using AuHub.Shared.Results;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Commands.CompleteLot;

public class CompleteLotCommandHandler
{
    private readonly ILotRepository _lotRepository;

    public CompleteLotCommandHandler(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public async Task<Result<CompleteLotResponse>> HandleAsync(
        CompleteLotCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lot = await _lotRepository.GetByIdAsync(command.LotId, cancellationToken);

            if (lot == null)
            {
                return Result.Failure<CompleteLotResponse>("Lot not found", 404);
            }

            // Проверка владельца
            if (lot.SellerId != userId)
            {
                return Result.Failure<CompleteLotResponse>("You are not the owner of this lot", 403);
            }

            // Вызываем domain метод Complete
            lot.Complete();

            // UpdateAsync не нужен - EF Core автоматически отслеживает изменения
            await _lotRepository.SaveChangesAsync(cancellationToken);

            var response = new CompleteLotResponse
            {
                Success = true,
                LotId = lot.Id,
                FinalPrice = lot.CurrentPrice,
                Message = "Lot completed successfully"
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
    public decimal FinalPrice { get; init; }
    public string Message { get; init; } = string.Empty;
}
