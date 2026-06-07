using AuHub.Shared.Contracts;
using AuHub.Shared.ValueObjects;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Auctions.Infrastructure.Data;

namespace Auctions.Infrastructure.BackgroundServices;

public class AuctionCompletionService : BackgroundService
{
    private readonly ILogger<AuctionCompletionService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public AuctionCompletionService(
        ILogger<AuctionCompletionService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auction Completion Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndCompleteExpiredAuctions(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking expired auctions");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Auction Completion Service stopped");
    }

    private async Task CheckAndCompleteExpiredAuctions(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var lotRepository = scope.ServiceProvider.GetRequiredService<ILotRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        var domainEventDispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
        var paymentClient = scope.ServiceProvider.GetRequiredService<IPaymentClient>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var context = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();

        var activeLots = await lotRepository.GetActiveLotsAsync(null, cancellationToken);
        var now = DateTime.UtcNow;
        var expiredLots = activeLots.Where(lot => lot.EndTime.HasValue && lot.EndTime.Value <= now).ToList();

        if (expiredLots.Any())
        {
            _logger.LogInformation("Found {Count} expired auctions to complete", expiredLots.Count);

            foreach (var lot in expiredLots)
            {
                try
                {
                    var previousStatus = lot.Status;
                    lot.Complete();
                    if (lot.WinnerId.HasValue)
                    {
                        lot.OpenDeliveryRequestWindow();
                    }

                    if (previousStatus != lot.Status)
                    {
                        var winnerName = lot.WinnerId.HasValue
                            ? $"User {lot.WinnerId.Value.ToString()[..8]}..."
                            : null;

                        // Process payment if there's a winner
                        if (lot.WinnerId.HasValue && lot.CurrentPrice > Money.Zero)
                        {
                            var chargeResult = await paymentClient.ChargeWinnerAsync(
                                lot.WinnerId.Value, lot.CurrentPrice.Amount, lot.Id, cancellationToken);

                            if (chargeResult.Success)
                            {
                                var commission = lot.CurrentPrice * 0.1m; // 10% platform commission
                                var sellerAmount = lot.CurrentPrice - commission;

                                var transferResult = await paymentClient.TransferToSellerAsync(
                                    lot.SellerId, sellerAmount.Amount, lot.Id, cancellationToken);

                                if (transferResult.Success)
                                {
                                    _logger.LogInformation(
                                        "Payment processed for auction {LotId}: charged winner {WinnerId}, transferred {Amount} to seller {SellerId}",
                                        lot.Id, lot.WinnerId.Value, sellerAmount, lot.SellerId);
                                }
                                else
                                {
                                    _logger.LogWarning(
                                        "Failed to transfer to seller for auction {LotId}: {Message}",
                                        lot.Id, transferResult.Message);
                                }
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Failed to charge winner for auction {LotId}: {Message}",
                                    lot.Id, chargeResult.Message);
                            }
                        }

                        // Notifications via Outbox (guaranteed delivery)
                        var outboxPayload = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            lotId = lot.Id, lotTitle = lot.Title,
                            winnerId = lot.WinnerId, winnerName = winnerName,
                            finalPrice = lot.CurrentPrice.Amount, sellerId = lot.SellerId
                        });

                        await outbox.AddAsync("AuctionCompleted", outboxPayload, cancellationToken);

                        await eventPublisher.PublishLotCompletedAsync(
                            lot.Id, lot.Title, lot.CurrentPrice.Amount, winnerName, cancellationToken);

                        await domainEventDispatcher.DispatchAllAsync(lot.DomainEvents, cancellationToken);
                        lot.ClearDomainEvents();

                        // Publish integration event via MassTransit
                        await publishEndpoint.Publish(new AuctionCompletedEvent
                        {
                            LotId = lot.Id,
                            LotTitle = lot.Title,
                            WinnerId = lot.WinnerId,
                            WinnerName = winnerName,
                            FinalPrice = lot.CurrentPrice.Amount,
                            SellerId = lot.SellerId
                        }, cancellationToken);

                        _logger.LogInformation(
                            "Completed auction {LotId} - {Title}. Final price: {Price}",
                            lot.Id, lot.Title, lot.CurrentPrice.Amount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to complete auction {LotId}", lot.Id);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
