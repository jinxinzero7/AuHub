using FastEndpoints;
using Identity.Domain.Interfaces;

namespace Identity.API.Endpoints.Users;

public class GetBannedUsersEndpoint : EndpointWithoutRequest<List<BannedUserResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetBannedUsersEndpoint(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public override void Configure()
    {
        Get("/api/auth/users/banned");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Get banned users (Admin only)";
            s.Description = "Retrieve all banned users.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var users = await _userRepository.GetBannedUsersAsync(ct);

        Response = users.Select(u => new BannedUserResponse
        {
            UserId = u.Id,
            Email = u.Email,
            Name = u.Name,
            BannedAt = u.BannedAt!.Value,
            Reason = u.BanReason ?? ""
        }).ToList();
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
