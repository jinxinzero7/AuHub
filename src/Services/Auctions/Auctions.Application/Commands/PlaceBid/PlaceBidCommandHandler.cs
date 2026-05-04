using Auctions.Domain.Common;
using Auctions.Domain.Interfaces;
using Auctions.Domain.Entities;

namespace Auctions.Application.Commands.PlaceBid;

public class PlaceBidCommandHandler
{
    private readonly ILotRepository _lotRepository;
    private readonly IBidRepository _bidRepository;

    public PlaceBidCommandHandler(ILotRepository lotRepository, IBidRepository bidRepository)
    {
        _lotRepository = lotRepository;
        _bidRepository = bidRepository;
    }

    public async Task<Result<PlaceBidResponse>> HandleAsync(
        PlaceBidCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lot = await _lotRepository.GetByIdAsync(command.LotId, cancellationToken);

            if (lot == null)
            {
                return Result.Failure<PlaceBidResponse>("Lot not found", 404);
            }

            // Валидация и обновление цены лота
            lot.PlaceBid(command.Amount);

            // Создание bid через domain factory
            var bid = Bid.Create(lot.Id, command.BidderId, command.Amount);

            // Сохранение bid через его репозиторий
            await _bidRepository.AddAsync(bid, cancellationToken);
            await _bidRepository.SaveChangesAsync(cancellationToken);

            var response = new PlaceBidResponse
            {
                Success = true,
                LotId = lot.Id,
                NewCurrentPrice = lot.CurrentPrice,
                Message = "Bid placed successfully"
            };

            return Result.Success(response);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<PlaceBidResponse>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return Result.Failure<PlaceBidResponse>($"Failed to place bid: {ex.Message}", 500);
        }
    }
}

public record PlaceBidResponse
{
    public bool Success { get; init; }
    public Guid LotId { get; init; }
    public decimal NewCurrentPrice { get; init; }
    public string Message { get; init; } = string.Empty;
}
