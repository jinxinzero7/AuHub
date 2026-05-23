using Auctions.Application.Commands.CompleteLot;
using FastEndpoints;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Lots;

public class CompleteLotEndpoint : EndpointWithoutRequest<CompleteLotResponse>
{
    private readonly CompleteLotCommandHandler _handler;

    public CompleteLotEndpoint(CompleteLotCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/complete");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Complete a lot manually (Owner only)";
            s.Description = "Manually complete an active auction lot. Only the owner can complete.";
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
        
        var command = new CompleteLotCommand
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
