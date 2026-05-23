using FastEndpoints;
using System.Security.Claims;
using Auctions.Domain.Interfaces;

namespace Auctions.API.Endpoints.Lots;

public class ShipLotEndpoint : Endpoint<ShipLotRequest>
{
    private readonly ILotRepository _lotRepository;

    public ShipLotEndpoint(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/ship");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Ship a lot (Seller only)";
            s.Description = "Mark lot as shipped with tracking number.";
        });
    }

    public override async Task HandleAsync(ShipLotRequest req, CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        if (string.IsNullOrWhiteSpace(req.TrackingNumber))
        {
            ThrowError("Tracking number is required", 400);
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

        if (lot.SellerId != userId)
        {
            ThrowError("Only seller can ship", 403);
            return;
        }

        lot.Ship(req.TrackingNumber);
        await _lotRepository.SaveChangesAsync(ct);

        Response = new { Success = true, Message = "Lot shipped" };
    }
}

public record ShipLotRequest
{
    public string TrackingNumber { get; init; } = string.Empty;
}
