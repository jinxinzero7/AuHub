using Auctions.Application.Queries.GetLots;
using FastEndpoints;

namespace Auctions.API.Endpoints.Admin;

public class GetPendingModerationEndpoint : EndpointWithoutRequest<List<LotResponse>>
{
    private readonly GetLotsQueryHandler _handler;

    public GetPendingModerationEndpoint(GetLotsQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/admin/lots/pending");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Get lots pending moderation (Admin only)";
            s.Description = "Retrieve all lots with Draft status awaiting admin approval.";
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

        // Filter only Draft status
        var pendingLots = result.Value.Lots
            .Where(l => l.Status == "Draft")
            .ToList();

        Response = pendingLots;
    }
}
