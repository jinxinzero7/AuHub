using Auctions.Application.Queries.GetMyBids;
using FastEndpoints;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Lots;

public class GetMyBidsEndpoint : EndpointWithoutRequest<GetMyBidsResponse>
{
    private readonly GetMyBidsQueryHandler _handler;

    public GetMyBidsEndpoint(GetMyBidsQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/bids/my");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Get my bids";
            s.Description = "Returns all bids placed by the current user, grouped by lot.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID in token", 401);
            return;
        }

        var query = new GetMyBidsQuery { UserId = userId };
        var result = await _handler.HandleAsync(query, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}
