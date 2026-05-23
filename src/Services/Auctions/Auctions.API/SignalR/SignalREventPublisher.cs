using Microsoft.AspNetCore.SignalR;
using Auctions.API.Hubs;
using Auctions.Application.Services;

namespace Auctions.API.SignalR;

public class SignalREventPublisher : IEventPublisher
{
    private readonly IHubContext<AuctionHub> _hubContext;

    public SignalREventPublisher(IHubContext<AuctionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PublishNewBidAsync(Guid lotId, decimal newPrice, string bidderName, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"lot-{lotId}").SendAsync("NewBidPlaced", new
        {
            lotId,
            newPrice,
            bidderName,
            timestamp = DateTime.UtcNow
        }, ct);
    }

    public async Task PublishLotCompletedAsync(Guid lotId, string title, decimal finalPrice, string? winnerName, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"lot-{lotId}").SendAsync("LotCompleted", new
        {
            lotId,
            title,
            finalPrice,
            winnerName,
            timestamp = DateTime.UtcNow
        }, ct);
    }

    public async Task PublishUserNotificationAsync(Guid userId, string type, string message, Guid? lotId, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"user-{userId}").SendAsync("NewNotification", new
        {
            userId,
            type,
            message,
            lotId,
            timestamp = DateTime.UtcNow
        }, ct);
    }
}
