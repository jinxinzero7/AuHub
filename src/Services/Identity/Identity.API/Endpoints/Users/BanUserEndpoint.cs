using FastEndpoints;
using Identity.Domain.Interfaces;

namespace Identity.API.Endpoints.Users;

public class BanUserEndpoint : Endpoint<BanUserRequest>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public BanUserEndpoint(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public override void Configure()
    {
        Post("/api/auth/users/{id}/ban");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Ban a user (Admin only)";
            s.Description = "Ban a user from the platform.";
        });
    }

    public override async Task HandleAsync(BanUserRequest req, CancellationToken ct)
    {
        var userId = Route<Guid>("id");

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
        {
            ThrowError("User not found", 404);
            return;
        }

        user.Ban(req.Reason);
        await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, ct);
        await _userRepository.SaveChangesAsync(ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        await HttpContext.Response.WriteAsJsonAsync(new { success = true }, ct);
    }
}

public record BanUserRequest
{
    public string Reason { get; init; } = string.Empty;
}
