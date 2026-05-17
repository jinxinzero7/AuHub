using Auctions.Domain.Interfaces;
using Auctions.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

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

        var activeLots = await lotRepository.GetActiveLotsAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var expiredLots = activeLots.Where(lot => lot.EndTime <= now).ToList();

        if (expiredLots.Any())
        {
            _logger.LogInformation("Found {Count} expired auctions to complete", expiredLots.Count);

            foreach (var lot in expiredLots)
            {
                try
                {
                    var previousStatus = lot.Status;
                    lot.Complete();

                    if (previousStatus != lot.Status)
                    {
                        var winnerName = lot.WinnerId.HasValue
                            ? $"User {lot.WinnerId.Value.ToString()[..8]}..."
                            : null;

                        await eventPublisher.PublishLotCompletedAsync(
                            lot.Id,
                            lot.Title,
                            lot.CurrentPrice,
                            winnerName,
                            cancellationToken);

                        _logger.LogInformation(
                            "Completed auction {LotId} - {Title}. Final price: {Price}",
                            lot.Id,
                            lot.Title,
                            lot.CurrentPrice);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to complete auction {LotId}", lot.Id);
                }
            }

            await lotRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
