using FastEndpoints;

namespace Auctions.API.Endpoints.Admin;

public class GetBannedUsersEndpoint : EndpointWithoutRequest<List<BannedUserResponse>>
{
    public override void Configure()
    {
        Get("/api/admin/users/banned");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Get banned users (Admin only)";
            s.Description = "Retrieve all banned users. TODO: Requires IsBanned field in User entity.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // TODO: Implement after adding IsBanned field to User entity in Identity service
        // For now, return empty list
        Response = new List<BannedUserResponse>();
    }
}

public record BannedUserResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime BannedAt { get; init; }
    public string Reason { get; init; } = string.Empty;
}
