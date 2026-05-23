using FastEndpoints;
using System.Security.Claims;
using Auctions.Domain.Interfaces;

namespace Auctions.API.Endpoints.Lots;

public class OpenDisputeEndpoint : Endpoint<OpenDisputeRequest>
{
    private readonly ILotRepository _lotRepository;

    public OpenDisputeEndpoint(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/dispute");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Open a dispute (Buyer only)";
            s.Description = "Open a dispute for a completed lot.";
        });
    }

    public override async Task HandleAsync(OpenDisputeRequest req, CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        if (string.IsNullOrWhiteSpace(req.Reason))
        {
            ThrowError("Dispute reason is required", 400);
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
            ThrowError("Only winner can open dispute", 403);
            return;
        }

        lot.OpenDispute(req.Reason);
        await _lotRepository.SaveChangesAsync(ct);

        Response = new { Success = true, Message = "Dispute opened" };
    }
}

public record OpenDisputeRequest
{
    public string Reason { get; init; } = string.Empty;
}
