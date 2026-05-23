using Auctions.Application.Commands.CancelLot;
using FastEndpoints;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Lots;

public class CancelLotEndpoint : EndpointWithoutRequest<CancelLotResponse>
{
    private readonly CancelLotCommandHandler _handler;

    public CancelLotEndpoint(CancelLotCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/cancel");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Cancel a lot (Owner only)";
            s.Description = "Cancel a draft or active auction lot. Only the owner can cancel.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID in token", 401);
            return;
        }
        
        var command = new CancelLotCommand
        {
            LotId = lotId
        };

        var result = await _handler.HandleAsync(command, userId, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}
