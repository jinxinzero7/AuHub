using FastEndpoints;
using Identity.Domain.Interfaces;

namespace Identity.API.Endpoints.Users;

public class GetPublicUserProfileEndpoint : EndpointWithoutRequest<PublicUserProfileResponse>
{
    private readonly IUserRepository _userRepository;

    public GetPublicUserProfileEndpoint(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public override void Configure()
    {
        Get("/api/auth/users/{id}/public-profile");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(Route<Guid>("id"), ct);
        if (user == null)
        {
            ThrowError("User not found", 404);
            return;
        }

        Response = new PublicUserProfileResponse
        {
            UserId = user.Id,
            Nickname = user.Nickname,
            Name = user.Name,
            DocumentVerificationStatus = user.DocumentVerificationStatus.ToString()
        };
    }
}

public record PublicUserProfileResponse
{
    public Guid UserId { get; init; }
    public string Nickname { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DocumentVerificationStatus { get; init; } = string.Empty;
}
