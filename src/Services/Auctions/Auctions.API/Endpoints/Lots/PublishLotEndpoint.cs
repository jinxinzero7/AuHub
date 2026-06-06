using Auctions.Application.Commands.PublishLot;
using FastEndpoints;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Lots;

public class PublishLotEndpoint : EndpointWithoutRequest<PublishLotResponse>
{
    private readonly PublishLotCommandHandler _handler;

    public PublishLotEndpoint(PublishLotCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/submit-for-moderation");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Submit a lot for moderation (Owner only)";
            s.Description = "Change lot status from Draft to PendingModeration. Only the owner can submit.";
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
        
        var command = new PublishLotCommand
        {
            LotId = lotId
        };

        var result = await _handler.HandleAsync(command, userId, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        Response = result.Value;
    }
}
