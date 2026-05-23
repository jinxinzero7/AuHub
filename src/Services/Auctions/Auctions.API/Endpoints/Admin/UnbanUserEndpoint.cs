using FastEndpoints;

namespace Auctions.API.Endpoints.Admin;

public class UnbanUserEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/admin/users/{id}/unban");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Unban a user (Admin only)";
            s.Description = "Remove ban from a user. TODO: Requires IsBanned field in User entity.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Route<Guid>("id");

        // TODO: Implement after adding IsBanned field to User entity in Identity service
        // This would require:
        // 1. Add IsBanned, BannedAt, BanReason fields to User entity
        // 2. Create UnbanUserCommand in Identity.Application
        // 3. Call Identity service via HTTP client
        
        HttpContext.Response.StatusCode = 200;
    }
}
