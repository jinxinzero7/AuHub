using Auctions.Application.Queries.GetLotById;
using FastEndpoints;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Lots;

public class GetLotByIdEndpoint : EndpointWithoutRequest<LotDetailResponse>
{
    private readonly GetLotByIdQueryHandler _handler;

    public GetLotByIdEndpoint(GetLotByIdQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/lots/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");
        var requesterUserId = GetRequesterUserId();
        var query = new GetLotByIdQuery
        {
            LotId = lotId,
            RequesterUserId = requesterUserId,
            RequesterIsAdmin = User.IsInRole("Admin")
        };

        var result = await _handler.HandleAsync(query, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }

    private Guid? GetRequesterUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
