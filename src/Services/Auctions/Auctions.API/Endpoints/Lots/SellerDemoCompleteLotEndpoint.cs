using Auctions.Application.Commands.CompleteLot;
using FastEndpoints;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Lots;

public class SellerDemoCompleteLotEndpoint : EndpointWithoutRequest<CompleteLotResponse>
{
    private readonly CompleteLotCommandHandler _handler;

    public SellerDemoCompleteLotEndpoint(CompleteLotCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/demo-complete");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Demo-complete a seller lot";
            s.Description = "Seller-only demo shortcut for showing the post-auction delivery flow without waiting for the scheduled end time.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID", 401);
            return;
        }

        var result = await _handler.HandleAsync(new CompleteLotCommand
        {
            LotId = Route<Guid>("id"),
            ActorUserId = userId,
            RequireSellerOwnership = true,
            RequireBid = true
        }, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        Response = result.Value;
    }
}
