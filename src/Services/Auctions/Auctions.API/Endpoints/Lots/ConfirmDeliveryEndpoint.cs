using FastEndpoints;
using System.Security.Claims;
using Auctions.Domain.Interfaces;

namespace Auctions.API.Endpoints.Lots;

public class ConfirmDeliveryEndpoint : EndpointWithoutRequest
{
    private readonly ILotRepository _lotRepository;

    public ConfirmDeliveryEndpoint(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/confirm-delivery");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Confirm delivery (Buyer only)";
            s.Description = "Confirm that the lot has been received.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID", 401);
            return;
        }

        var lot = await _lotRepository.GetByIdAsync(lotId, ct);
        if (lot == null)
        {
            ThrowError("Lot not found", 404);
            return;
        }

        if (lot.WinnerId != userId)
        {
            ThrowError("Only winner can confirm delivery", 403);
            return;
        }

        lot.ConfirmDelivery();
        await _lotRepository.SaveChangesAsync(ct);

        Response = new { Success = true, Message = "Delivery confirmed" };
    }
}
