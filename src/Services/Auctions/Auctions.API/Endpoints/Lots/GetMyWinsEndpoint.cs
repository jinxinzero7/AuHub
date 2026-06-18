using System.Security.Claims;
using Auctions.Application.Queries.GetLots;
using FastEndpoints;

namespace Auctions.API.Endpoints.Lots;

public class GetMyWinsEndpoint : EndpointWithoutRequest<PaginatedLotsResponse>
{
    private readonly GetLotsQueryHandler _handler;

    public GetMyWinsEndpoint(GetLotsQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/me/wins");
        Roles("User");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = GetUserId();
        var page = Query<int>("page", isRequired: false);
        var pageSize = Query<int>("pageSize", isRequired: false);
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var result = await _handler.HandleAsync(new GetLotsQuery
        {
            WinnerId = userId.ToString(),
            IncludePrivateDeliveryDetails = true,
            Page = page,
            PageSize = pageSize
        }, ct);

        if (result.IsFailure)
            ThrowError(result.Error, result.StatusCode);

        Response = result.Value;
    }

    private Guid GetUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(value, out var userId))
            ThrowError("Invalid user ID in token", 401);

        return userId;
    }
}
