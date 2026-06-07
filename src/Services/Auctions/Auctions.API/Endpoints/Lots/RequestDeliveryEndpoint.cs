using FastEndpoints;
using System.Security.Claims;
using Auctions.Domain.Interfaces;
using Auctions.Domain.Enums;

namespace Auctions.API.Endpoints.Lots;

public class RequestDeliveryEndpoint : Endpoint<RequestDeliveryRequest>
{
    private readonly ILotRepository _lotRepository;

    public RequestDeliveryEndpoint(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/delivery-request");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Request delivery (Winner only)";
            s.Description = "Select delivery provider and destination for a completed lot.";
        });
    }

    public override async Task HandleAsync(RequestDeliveryRequest req, CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        if (!Enum.TryParse<DeliveryProvider>(req.Provider, ignoreCase: true, out var provider))
        {
            ThrowError("Unsupported delivery provider", 400);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Address))
        {
            ThrowError("Delivery address is required", 400);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.RecipientName))
        {
            ThrowError("Recipient name is required", 400);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.RecipientPhone))
        {
            ThrowError("Recipient phone is required", 400);
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
            ThrowError("Only winner can request delivery", 403);
            return;
        }

        try
        {
            lot.RequestDelivery(provider, req.Address, req.RecipientName, req.RecipientPhone);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, 400);
            return;
        }

        await _lotRepository.SaveChangesAsync(ct);

        Response = new { Success = true, Message = "Delivery requested" };
    }
}

public record RequestDeliveryRequest
{
    public string Provider { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public string RecipientPhone { get; init; } = string.Empty;
}
