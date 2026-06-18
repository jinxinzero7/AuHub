using System.Security.Claims;
using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Auctions.API.Hubs;

[Authorize]
public class AuctionHub : Hub
{
    private readonly ILotRepository _lotRepository;

    public AuctionHub(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public async Task JoinLotGroup(Guid lotId)
    {
        var lot = await _lotRepository.GetByIdAsync(lotId, Context.ConnectionAborted);
        if (lot is null || lot.IsDeleted || lot.Status != LotStatus.Active)
            throw new HubException("Lot is not publicly available");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"lot-{lotId}");
    }

    public async Task LeaveLotGroup(Guid lotId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"lot-{lotId}");
    }

    public async Task JoinUserGroup()
    {
        var userId = GetAuthenticatedUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }

    public async Task LeaveUserGroup()
    {
        var userId = GetAuthenticatedUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
    }

    private Guid GetAuthenticatedUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId))
            throw new HubException("Authenticated user identifier is missing");

        return userId;
    }
}
