using AuHub.Shared.Results;
using Auctions.Domain.Interfaces;
using Auctions.Domain.Entities;
using Auctions.Application.Services;

namespace Auctions.Application.Commands.PlaceBid;

public class PlaceBidCommandHandler
{
    private readonly ILotRepository _lotRepository;
    private readonly IBidRepository _bidRepository;
    private readonly IEventPublisher _eventPublisher;

    public PlaceBidCommandHandler(
        ILotRepository lotRepository,
        IBidRepository bidRepository,
        IEventPublisher eventPublisher)
    {
        _lotRepository = lotRepository;
        _bidRepository = bidRepository;
        _eventPublisher = eventPublisher;
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

            if (lot.SellerId == command.BidderId)
            {
                return Result.Failure<PlaceBidResponse>("You cannot bid on your own lot", 403);
            }

            lot.PlaceBid(command.Amount);

            var bid = Bid.Create(lot.Id, command.BidderId, command.Amount);

            await _bidRepository.AddAsync(bid, cancellationToken);
            await _bidRepository.SaveChangesAsync(cancellationToken);
            await _lotRepository.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishNewBidAsync(
                lot.Id,
                lot.CurrentPrice,
                command.BidderName,
                cancellationToken);

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
