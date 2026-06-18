using Auctions.Application.Queries.GetAdminUserActivity;
using FastEndpoints;

namespace Auctions.API.Endpoints.Admin;

public class GetAdminUserActivityEndpoint : EndpointWithoutRequest<AdminUserActivityResponse>
{
    private const int DefaultPageSize = 20;
    private readonly GetAdminUserActivityQueryHandler _handler;

    public GetAdminUserActivityEndpoint(GetAdminUserActivityQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/admin/users/{userId}/activity");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Get admin user marketplace activity";
            s.Description = "Returns Auctions-owned moderation summaries without delivery recipient details.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var page = Query<int>("page", false);
        var pageSize = Query<int>("pageSize", false);
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = DefaultPageSize;

        var result = await _handler.HandleAsync(Route<Guid>("userId"), page, pageSize, ct);
        Response = result.Value;
    }
}
