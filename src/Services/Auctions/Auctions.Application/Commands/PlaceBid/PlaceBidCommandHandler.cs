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

        // Idempotency check
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

                // Check balance via Payment Service
                var balanceResult = await _paymentClient.GetBalanceAsync(command.BidderId, cancellationToken);
                if (!balanceResult.Success)
                    return Result.Failure<PlaceBidResponse>("Payment service unavailable", 503);
                if (balanceResult.Balance < command.Amount.Amount)
                    return Result.Failure<PlaceBidResponse>(
                        $"Insufficient funds. Required: {command.Amount}, Available: {balanceResult.Balance:C2}", 400);

                // Store previous bidder info for releasing funds (before PlaceBid updates CurrentPrice)
                var previousBid = lot.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
                var previousBidderId = previousBid?.BidderId;
                var previousBidAmount = lot.CurrentPrice;

                // Reserve funds for new bid
                var reserveResult = await _paymentClient.ReserveFundsAsync(
                    command.BidderId, command.Amount.Amount, lot.Id, cancellationToken);
                if (!reserveResult.Success)
                    return Result.Failure<PlaceBidResponse>("Failed to reserve funds", 503);

                lot.PlaceBid(command.Amount, command.BidderId, command.BidderName);

                // Sniper protection: extend auction if bid placed in last 30 seconds
                if (lot.EndTime.HasValue && (lot.EndTime.Value - DateTime.UtcNow).TotalSeconds < 30)
                {
                    lot.ExtendEndTime(TimeSpan.FromMinutes(2));
                }

                var bid = Bid.Create(lot.Id, command.BidderId, command.Amount, command.IdempotencyKey);
                await _bidRepository.AddAsync(bid, cancellationToken);

                // Release funds for previous bidder (if exists and different from current)
                if (previousBidderId.HasValue && previousBidderId.Value != command.BidderId)
                {
                    await _paymentClient.ReleaseFundsAsync(
                        previousBidderId.Value, previousBidAmount.Amount, lot.Id, cancellationToken);
                }

                // Outbox for async notification dispatch
                var outboxPayload = JsonSerializer.Serialize(new
                {
                    lotId = lot.Id, sellerId = lot.SellerId, bidderId = command.BidderId,
                    bidderName = command.BidderName, amount = command.Amount.Amount, lotTitle = lot.Title
                });
                await _outbox.AddAsync("BidPlaced", outboxPayload, cancellationToken);

                await _bidRepository.SaveChangesAsync(cancellationToken);
                await _lotRepository.SaveChangesAsync(cancellationToken);

                await _domainEventDispatcher.DispatchAllAsync(lot.DomainEvents, cancellationToken);
                lot.ClearDomainEvents();

                await _eventPublisher.PublishNewBidAsync(lot.Id, lot.CurrentPrice.Amount, command.BidderName, cancellationToken);

                // Publish integration event via MassTransit
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
            catch (InvalidOperationException ex)
            {
                return Result.Failure<PlaceBidResponse>(ex.Message, 400);
            }
            catch (Exception ex)
            {
                return Result.Failure<PlaceBidResponse>($"Failed to place bid: {ex.Message}", 500);
            }
        }

        return Result.Failure<PlaceBidResponse>("Too many concurrent bids, please try again", 409);
    }
}

public record PlaceBidResponse
{
    public bool Success { get; init; }
    public Guid LotId { get; init; }
    public Money NewCurrentPrice { get; init; } = Money.Zero;
    public string Message { get; init; } = string.Empty;
}
