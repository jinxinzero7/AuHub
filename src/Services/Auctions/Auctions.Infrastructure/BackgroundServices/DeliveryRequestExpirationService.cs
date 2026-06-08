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

        foreach (var lot in lots)
        {
            lot.ExpireDeliveryRequest();
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Expired {Count} overdue delivery requests", lots.Count);
    }
}
