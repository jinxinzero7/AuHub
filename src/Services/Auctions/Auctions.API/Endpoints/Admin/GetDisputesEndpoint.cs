using Auctions.Application.Queries.GetLots;
using FastEndpoints;

namespace Auctions.API.Endpoints.Admin;

public class GetDisputesEndpoint : EndpointWithoutRequest<List<LotResponse>>
{
    private readonly GetLotsQueryHandler _handler;

    public GetDisputesEndpoint(GetLotsQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/admin/disputes");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Get lots with disputes (Admin only)";
            s.Description = "Retrieve all lots with Disputed status requiring admin resolution.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var query = new GetLotsQuery
        {
            Page = 1,
            PageSize = 1000,
            OnlyActive = false,
            IncludeDrafts = true,
            IncludePrivateDeliveryDetails = true
        };

        var result = await _handler.HandleAsync(query, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        // Filter only Disputed status
        var disputedLots = result.Value.Lots
            .Where(l => l.Status == "Disputed")
            .ToList();

        Response = disputedLots;
    }
}
