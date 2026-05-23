using Microsoft.AspNetCore.SignalR;

namespace Auctions.API.Hubs;

public class AuctionHub : Hub
{
    public async Task NewBidPlaced(Guid lotId, decimal newPrice, string bidderName)
    {
        await Clients.Group($"lot-{lotId}").SendAsync("NewBidPlaced", new
        {
            lotId,
            newPrice,
            bidderName,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task LotCompleted(Guid lotId, string title, decimal finalPrice, string? winnerName)
    {
        await Clients.Group($"lot-{lotId}").SendAsync("LotCompleted", new
        {
            lotId,
            title,
            finalPrice,
            winnerName,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task JoinLotGroup(Guid lotId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"lot-{lotId}");
    }

    public async Task LeaveLotGroup(Guid lotId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lot-{lotId}");
    }

    public async Task JoinUserGroup(Guid userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }

    public async Task LeaveUserGroup(Guid userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
    }
}
