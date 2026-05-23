using FastEndpoints;

namespace Auctions.API.Endpoints.Admin;

public class BanUserEndpoint : Endpoint<BanUserRequest>
{
    public override void Configure()
    {
        Post("/api/admin/users/{id}/ban");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Ban a user (Admin only)";
            s.Description = "Ban a user from the platform. TODO: Requires IsBanned field in User entity.";
        });
    }

    public override async Task HandleAsync(BanUserRequest req, CancellationToken ct)
    {
        var userId = Route<Guid>("id");

        // TODO: Implement after adding IsBanned field to User entity in Identity service
        // This would require:
        // 1. Add IsBanned, BannedAt, BanReason fields to User entity
        // 2. Create BanUserCommand in Identity.Application
        // 3. Call Identity service via HTTP client
        
        HttpContext.Response.StatusCode = 200;
    }
}

public record BanUserRequest
{
    public string Reason { get; init; } = string.Empty;
}
