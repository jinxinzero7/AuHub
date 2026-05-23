using Auctions.Application.Queries.GetLots;
using FastEndpoints;

namespace Auctions.API.Endpoints.Admin;

public class GetFrozenLotsEndpoint : EndpointWithoutRequest<List<LotResponse>>
{
    private readonly GetLotsQueryHandler _handler;

    public GetFrozenLotsEndpoint(GetLotsQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/admin/lots/frozen");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Get frozen lots (Admin only)";
            s.Description = "Retrieve all lots with Frozen status (suspended by admin).";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var query = new GetLotsQuery
        {
            Page = 1,
            PageSize = 1000,
            OnlyActive = false,
            IncludeDrafts = true
        };

        var result = await _handler.HandleAsync(query, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        // Filter only Frozen status
        var frozenLots = result.Value.Lots
            .Where(l => l.Status == "Frozen")
            .ToList();

        Response = frozenLots;
    }
}
