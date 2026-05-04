using Auctions.Application.Commands.Auth.Login;
using FastEndpoints;

namespace Auctions.API.Endpoints.Auth;

public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    private readonly LoginCommandHandler _handler;

    public LoginEndpoint(LoginCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Login user";
            s.Description = "Authenticates user and returns JWT tokens";
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var command = new LoginCommand
        {
            Email = req.Email,
            Password = req.Password
        };

        var result = await _handler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}

public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
