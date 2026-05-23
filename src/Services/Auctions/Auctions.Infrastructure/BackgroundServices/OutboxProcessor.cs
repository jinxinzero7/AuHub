using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Auctions.Domain.Entities;
using Auctions.Infrastructure.Data;
using System.Text.Json;

namespace Auctions.Infrastructure.BackgroundServices;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private const int MaxRetries = 3;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();

        var pendingMessages = await context.OutboxMessages
            .Where(o => o.ProcessedAt == null && o.RetryCount < MaxRetries)
            .OrderBy(o => o.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            try
            {
                await DispatchMessageAsync(message, scope.ServiceProvider, cancellationToken);
                message.MarkProcessed();
                _logger.LogInformation("Outbox message {Type} processed", message.Type);
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                _logger.LogError(ex, "Failed to process outbox message {Type} (retry {Retry})",
                    message.Type, message.RetryCount);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchMessageAsync(
        OutboxMessage message,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var payload = JsonDocument.Parse(message.Payload);

        switch (message.Type)
        {
            case "BidPlaced":
                await HandleBidPlacedAsync(payload, serviceProvider, cancellationToken);
                break;
            case "AuctionCompleted":
                await HandleAuctionCompletedAsync(payload, serviceProvider, cancellationToken);
                break;
            default:
                _logger.LogWarning("Unknown outbox message type: {Type}", message.Type);
                break;
        }
    }

    private async Task HandleBidPlacedAsync(
        JsonDocument payload,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var notificationClient = serviceProvider.GetRequiredService<Auctions.Application.Services.INotificationClient>();

        var sellerId = payload.RootElement.GetProperty("sellerId").GetGuid();
        var bidderName = payload.RootElement.GetProperty("bidderName").GetString() ?? "Unknown";
        var amount = payload.RootElement.GetProperty("amount").GetDecimal();
        var lotTitle = payload.RootElement.GetProperty("lotTitle").GetString() ?? "";

        await notificationClient.SendNotificationAsync(
            sellerId,
            Auctions.Domain.Enums.NotificationType.NewBid,
            "Новая ставка на ваш лот",
            $"{bidderName} сделал ставку {amount:C} на лот \"{lotTitle}\"",
            cancellationToken);
    }

    private async Task HandleAuctionCompletedAsync(
        JsonDocument payload,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var notificationClient = serviceProvider.GetRequiredService<Auctions.Application.Services.INotificationClient>();

        var winnerId = payload.RootElement.GetProperty("winnerId").GetGuid();
        var lotTitle = payload.RootElement.GetProperty("lotTitle").GetString() ?? "";
        var finalPrice = payload.RootElement.GetProperty("finalPrice").GetDecimal();

        await notificationClient.SendNotificationAsync(
            winnerId,
            Auctions.Domain.Enums.NotificationType.WonAuction,
            "Вы выиграли аукцион!",
            $"Поздравляем! Вы выиграли лот \"{lotTitle}\" с финальной ставкой {finalPrice:C}",
            cancellationToken);
    }
}
