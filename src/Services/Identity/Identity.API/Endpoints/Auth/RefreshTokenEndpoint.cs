using Identity.Application.Commands.Auth.RefreshToken;
using FastEndpoints;

namespace Identity.API.Endpoints.Auth;

public class RefreshTokenEndpoint : Endpoint<RefreshTokenRequest, RefreshTokenResponse>
{
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenEndpoint(RefreshTokenCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/auth/refresh");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Refresh access token";
            s.Description = "Generates new JWT tokens using refresh token";
        });
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var command = new RefreshTokenCommand
        {
            RefreshToken = req.RefreshToken
        };

        var result = await _handler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}

public record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
