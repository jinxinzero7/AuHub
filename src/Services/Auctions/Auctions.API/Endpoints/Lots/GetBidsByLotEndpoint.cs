using Auctions.Application.Queries.GetBidsByLot;
using FastEndpoints;
using System.Security.Claims;

namespace Auctions.API.Endpoints.Lots;

public class GetBidsByLotEndpoint : EndpointWithoutRequest<GetBidsResponse>
{
    private readonly GetBidsByLotQueryHandler _handler;

    public GetBidsByLotEndpoint(GetBidsByLotQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/lots/{id}/bids");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get bids for a lot";
            s.Description = "Returns all bids placed on a specific lot";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");
        
        var query = new GetBidsByLotQuery
        {
            LotId = lotId,
            RequesterUserId = GetRequesterUserId(),
            RequesterIsAdmin = User.IsInRole("Admin")
        };

        var result = await _handler.HandleAsync(query, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = new GetBidsResponse
        {
            Success = true,
            Bids = result.Value
        };
    }

    private Guid? GetRequesterUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

public record GetBidsResponse
{
    public bool Success { get; init; }
    public List<BidResponse> Bids { get; init; } = new();
}
