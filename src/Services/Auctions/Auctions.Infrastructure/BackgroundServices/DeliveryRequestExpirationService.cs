using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auctions.Infrastructure.BackgroundServices;

public class DeliveryRequestExpirationService : BackgroundService
{
    private readonly ILogger<DeliveryRequestExpirationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public DeliveryRequestExpirationService(
        ILogger<DeliveryRequestExpirationService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Delivery Request Expiration Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireOverdueDeliveryRequests(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while expiring delivery requests");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Delivery Request Expiration Service stopped");
    }

    private async Task ExpireOverdueDeliveryRequests(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuctionsDbContext>();
        var settlementService = scope.ServiceProvider.GetRequiredService<AuctionSettlementService>();
        var now = DateTime.UtcNow;

        var lots = await context.Lots
            .Where(lot =>
                lot.Status == LotStatus.DeliveryRequestPending &&
                lot.DeliveryRequestDeadlineAt.HasValue &&
                lot.DeliveryRequestDeadlineAt.Value <= now)
            .ToListAsync(cancellationToken);

        if (lots.Count == 0)
        {
            return;
        }

        var expiredCount = 0;
        foreach (var lot in lots)
        {
            var refundResult = await settlementService.RefundWinnerAsync(lot, cancellationToken);
            if (refundResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to refund winner for expired delivery request {LotId}: {Error}",
                    lot.Id,
                    refundResult.Error);
                continue;
            }

            lot.ExpireDeliveryRequest();
            expiredCount++;
        }

        if (expiredCount == 0)
        {
            return;
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Expired {Count} overdue delivery requests", expiredCount);
    }
}
