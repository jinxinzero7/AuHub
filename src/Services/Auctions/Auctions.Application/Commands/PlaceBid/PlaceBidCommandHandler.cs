using AuHub.Shared.Contracts;
using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using Auctions.Domain.Entities;
using Auctions.Application.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Auctions.Application.Commands.PlaceBid;

public class PlaceBidCommandHandler
{
    private readonly ILotRepository _lotRepository;
    private readonly IBidRepository _bidRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IOutbox _outbox;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IPaymentClient _paymentClient;
    private readonly IPublishEndpoint _publishEndpoint;

    public PlaceBidCommandHandler(
        ILotRepository lotRepository,
        IBidRepository bidRepository,
        IEventPublisher eventPublisher,
        IOutbox outbox,
        IDomainEventDispatcher domainEventDispatcher,
        IPaymentClient paymentClient,
        IPublishEndpoint publishEndpoint)
    {
        _lotRepository = lotRepository;
        _bidRepository = bidRepository;
        _eventPublisher = eventPublisher;
        _outbox = outbox;
        _domainEventDispatcher = domainEventDispatcher;
        _paymentClient = paymentClient;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<PlaceBidResponse>> HandleAsync(
        PlaceBidCommand command,
        CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        var fundsReserved = false;
        var reservedLotId = Guid.Empty;

        if (command.IdempotencyKey.HasValue)
        {
            var existingBid = await _bidRepository.GetByIdempotencyKeyAsync(command.IdempotencyKey.Value, cancellationToken);
            if (existingBid != null)
            {
                return Result.Success(new PlaceBidResponse
                {
                    Success = true,
                    LotId = existingBid.LotId,
                    NewCurrentPrice = existingBid.Amount,
                    Message = "Bid already exists (idempotent retry)"
                });
            }
        }

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var lot = await _lotRepository.GetByIdAsync(command.LotId, cancellationToken);
                if (lot == null)
                    return Result.Failure<PlaceBidResponse>("Lot not found", 404);
                if (lot.SellerId == command.BidderId)
                    return Result.Failure<PlaceBidResponse>("You cannot bid on your own lot", 403);
                if (command.Amount <= lot.CurrentPrice)
                    return Result.Failure<PlaceBidResponse>("Bid amount must be higher than current price", 400);

                var balanceResult = await _paymentClient.GetBalanceAsync(command.BidderId, cancellationToken);
                if (!balanceResult.Success)
                    return Result.Failure<PlaceBidResponse>("Payment service unavailable", 503);
                if (balanceResult.Balance < command.Amount.Amount)
                    return Result.Failure<PlaceBidResponse>(
                        $"Insufficient funds. Required: {command.Amount}, Available: {balanceResult.Balance:C2}", 400);

                var previousBid = lot.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
                var previousBidderId = previousBid?.BidderId;
                var previousBidAmount = lot.CurrentPrice;

                if (!fundsReserved)
                {
                    var reserveResult = await _paymentClient.ReserveFundsAsync(
                        command.BidderId, command.Amount.Amount, lot.Id, cancellationToken);
                    if (!reserveResult.Success)
                        return Result.Failure<PlaceBidResponse>("Failed to reserve funds", 503);

                    fundsReserved = true;
                    reservedLotId = lot.Id;
                }

                lot.PlaceBid(command.Amount, command.BidderId, command.BidderName);

                lot.ApplySniperProtection(DateTime.UtcNow);

                var bid = Bid.Create(lot.Id, command.BidderId, command.Amount, command.IdempotencyKey);
                await _bidRepository.AddAsync(bid, cancellationToken);

                var outboxPayload = JsonSerializer.Serialize(new
                {
                    lotId = lot.Id, sellerId = lot.SellerId, bidderId = command.BidderId,
                    bidderName = command.BidderName, amount = command.Amount.Amount, lotTitle = lot.Title
                });
                await _outbox.AddAsync("BidPlaced", outboxPayload, cancellationToken);

                if (previousBidderId.HasValue && previousBidderId.Value != command.BidderId)
                {
                    var releasePayload = JsonSerializer.Serialize(new
                    {
                        userId = previousBidderId.Value,
                        amount = previousBidAmount.Amount,
                        lotId = lot.Id
                    });
                    await _outbox.AddAsync("ReleasePreviousBidderFunds", releasePayload, cancellationToken);
                }

                await _bidRepository.SaveChangesAsync(cancellationToken);
                await _lotRepository.SaveChangesAsync(cancellationToken);

                if (previousBidderId.HasValue && previousBidderId.Value != command.BidderId)
                {
                    await _paymentClient.ReleaseFundsAsync(
                        previousBidderId.Value, previousBidAmount.Amount, lot.Id, cancellationToken);
                }

                await _domainEventDispatcher.DispatchAllAsync(lot.DomainEvents, cancellationToken);
                lot.ClearDomainEvents();

                await _eventPublisher.PublishNewBidAsync(lot.Id, lot.CurrentPrice.Amount, command.BidderName, cancellationToken);

                await _publishEndpoint.Publish(new BidPlacedEvent
                {
                    LotId = lot.Id,
                    BidderId = command.BidderId,
                    BidderName = command.BidderName,
                    Amount = command.Amount.Amount,
                    SellerId = lot.SellerId,
                    LotTitle = lot.Title
                }, cancellationToken);

                return Result.Success(new PlaceBidResponse
                {
                    Success = true, LotId = lot.Id, NewCurrentPrice = lot.CurrentPrice,
                    Message = "Bid placed successfully"
                });
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries)
            {
                await Task.Delay(100 * attempt, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (fundsReserved)
                {
                    await ReleaseReservedFundsAsync(command, reservedLotId, cancellationToken);
                }

                return Result.Failure<PlaceBidResponse>("Too many concurrent bids, please try again", 409);
            }
            catch (InvalidOperationException ex)
            {
                if (fundsReserved)
                {
                    await ReleaseReservedFundsAsync(command, reservedLotId, cancellationToken);
                }

                return Result.Failure<PlaceBidResponse>(ex.Message, 400);
            }
            catch (Exception ex)
            {
                if (fundsReserved)
                {
                    await ReleaseReservedFundsAsync(command, reservedLotId, cancellationToken);
                }

                return Result.Failure<PlaceBidResponse>($"Failed to place bid: {ex.Message}", 500);
            }
        }

        return Result.Failure<PlaceBidResponse>("Too many concurrent bids, please try again", 409);
    }

    private Task<PaymentResult> ReleaseReservedFundsAsync(
        PlaceBidCommand command,
        Guid lotId,
        CancellationToken cancellationToken)
    {
        return _paymentClient.ReleaseFundsAsync(
            command.BidderId,
            command.Amount.Amount,
            lotId,
            cancellationToken);
    }
}

public record PlaceBidResponse
{
    public bool Success { get; init; }
    public Guid LotId { get; init; }
    public Money NewCurrentPrice { get; init; } = Money.Zero;
    public string Message { get; init; } = string.Empty;
}
