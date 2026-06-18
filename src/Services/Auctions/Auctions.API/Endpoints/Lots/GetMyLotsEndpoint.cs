using System.Security.Claims;
using Auctions.Application.Queries.GetLots;
using FastEndpoints;

namespace Auctions.API.Endpoints.Lots;

public class GetMyLotsEndpoint : EndpointWithoutRequest<PaginatedLotsResponse>
{
    private readonly GetLotsQueryHandler _handler;

    public GetMyLotsEndpoint(GetLotsQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/me/lots");
        Roles("User");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = GetUserId();
        var (page, pageSize) = GetPagination();
        var result = await _handler.HandleAsync(new GetLotsQuery
        {
            SellerId = userId.ToString(),
            IncludeDrafts = true,
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

    private (int Page, int PageSize) GetPagination()
    {
        var page = Query<int>("page", isRequired: false);
        var pageSize = Query<int>("pageSize", isRequired: false);
        return (page < 1 ? 1 : page, pageSize is < 1 or > 100 ? 20 : pageSize);
    }
}
