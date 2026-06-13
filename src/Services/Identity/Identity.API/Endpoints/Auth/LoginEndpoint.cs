using Identity.Application.Commands.Auth.Login;
using FastEndpoints;

namespace Identity.API.Endpoints.Auth;

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
        var errors = new List<string>();
        
        var identifier = req.GetIdentifier();

        if (string.IsNullOrWhiteSpace(identifier))
            errors.Add("Email or phone number is required");
        if (string.IsNullOrEmpty(req.Password))
            errors.Add("Password is required");

        if (errors.Any())
        {
            ThrowError(string.Join("; ", errors), 400);
            return;
        }

        var command = new LoginCommand
        {
            Identifier = identifier,
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
    public string Identifier { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    public string GetIdentifier()
    {
        return string.IsNullOrWhiteSpace(Identifier)
            ? Email
            : Identifier;
    }
}
