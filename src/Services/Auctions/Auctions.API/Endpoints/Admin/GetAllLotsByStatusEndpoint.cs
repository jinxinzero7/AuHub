using Auctions.Application.Queries.GetLots;
using Auctions.Domain.Entities;
using FastEndpoints;

namespace Auctions.API.Endpoints.Admin;

public class GetAllLotsByStatusEndpoint : EndpointWithoutRequest<List<LotResponse>>
{
    private readonly GetLotsQueryHandler _handler;

    public GetAllLotsByStatusEndpoint(GetLotsQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/admin/lots");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Get all lots by status (Admin only)";
            s.Description = "Retrieve all lots filtered by status. Query param: ?status=Draft|Approved|Rejected|Active|Frozen|Completed";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var statusParam = Query<string>("status", isRequired: false);
        
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

        var lots = result.Value.Lots;

        // Filter by status if provided
        if (!string.IsNullOrEmpty(statusParam))
        {
            lots = lots.Where(l => l.Status.Equals(statusParam, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        Response = lots;
    }
}
