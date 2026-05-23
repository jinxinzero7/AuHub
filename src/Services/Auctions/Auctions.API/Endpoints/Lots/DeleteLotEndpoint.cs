using Auctions.Domain.Interfaces;
using FastEndpoints;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Lots;

public class DeleteLotEndpoint : EndpointWithoutRequest<DeleteLotResponse>
{
    private readonly ILotRepository _lotRepository;

    public DeleteLotEndpoint(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public override void Configure()
    {
        Delete("/api/lots/{id}");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Delete a lot (soft delete)";
            s.Description = "Soft delete a lot. Only owner can delete.";
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

        if (lot.SellerId != userId)
        {
            ThrowError("Only lot owner can delete", 403);
            return;
        }

        lot.SoftDelete(userId);
        await _lotRepository.SaveChangesAsync(ct);

        Response = new DeleteLotResponse { Success = true, Message = "Lot deleted" };
    }
}

public record DeleteLotResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}
