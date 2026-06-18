using FastEndpoints;
using Identity.Application.Queries.GetAdminUserDetail;

namespace Identity.API.Endpoints.Users;

public class GetAdminUserDetailEndpoint : EndpointWithoutRequest<AdminUserDetailResponse>
{
    private readonly GetAdminUserDetailQueryHandler _handler;

    public GetAdminUserDetailEndpoint(GetAdminUserDetailQueryHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Get("/api/auth/users/{id}");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Get admin user detail";
            s.Description = "Returns moderation-safe Identity profile and document verification metadata.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _handler.HandleAsync(Route<Guid>("id"), ct);
        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        Response = result.Value;
    }
}
