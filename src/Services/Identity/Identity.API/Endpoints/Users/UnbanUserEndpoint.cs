using FastEndpoints;
using Identity.Domain.Interfaces;

namespace Identity.API.Endpoints.Users;

public class UnbanUserEndpoint : EndpointWithoutRequest
{
    private readonly IUserRepository _userRepository;

    public UnbanUserEndpoint(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public override void Configure()
    {
        Post("/api/auth/users/{id}/unban");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Unban a user (Admin only)";
            s.Description = "Remove ban from a user.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Route<Guid>("id");

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            ThrowError("User not found", 404);
            return;
        }

        user.Unban();
        await _userRepository.SaveChangesAsync(ct);

        await HttpContext.Response.WriteAsJsonAsync(new { success = true }, ct);
    }
}
