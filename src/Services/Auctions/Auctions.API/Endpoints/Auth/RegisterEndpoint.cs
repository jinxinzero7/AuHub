using Auctions.Application.Commands.Auth.Register;
using FastEndpoints;

namespace Auctions.API.Endpoints.Auth;

public class RegisterEndpoint : Endpoint<RegisterRequest, RegisterResponse>
{
    private readonly RegisterCommandHandler _handler;

    public RegisterEndpoint(RegisterCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/auth/register");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Register a new user";
            s.Description = "Creates a new user account and returns JWT tokens";
        });
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var command = new RegisterCommand
        {
            Email = req.Email,
            Password = req.Password,
            Name = req.Name,
            Role = req.Role
        };

        var result = await _handler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = result.Value;
    }
}

public record RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Domain.Entities.UserRole Role { get; init; } = Domain.Entities.UserRole.User;
}
