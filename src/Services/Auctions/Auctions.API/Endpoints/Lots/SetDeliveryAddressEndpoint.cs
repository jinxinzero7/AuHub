using FastEndpoints;
using System.Security.Claims;
using Auctions.Domain.Interfaces;

namespace Auctions.API.Endpoints.Lots;

public class SetDeliveryAddressEndpoint : Endpoint<SetDeliveryAddressRequest>
{
    private readonly ILotRepository _lotRepository;

    public SetDeliveryAddressEndpoint(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/delivery-address");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Set delivery address (Winner only)";
            s.Description = "Set delivery address for a completed lot.";
        });
    }

    public override async Task HandleAsync(SetDeliveryAddressRequest req, CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        if (string.IsNullOrWhiteSpace(req.Address))
        {
            ThrowError("Delivery address is required", 400);
            return;
        }

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
            ThrowError("Only winner can set delivery address", 403);
            return;
        }

        lot.SetDeliveryAddress(req.Address);
        await _lotRepository.SaveChangesAsync(ct);

        Response = new { Success = true, Message = "Delivery address set" };
    }
}

public record SetDeliveryAddressRequest
{
    public string Address { get; init; } = string.Empty;
}
