using Identity.Application.Commands.Auth.Register;
using FastEndpoints;

namespace Identity.API.Endpoints.Auth;

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
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(req.Email) || !req.Email.Contains('@') || !req.Email.Contains('.'))
            errors.Add("Invalid email format");
        if (string.IsNullOrEmpty(req.Password) || req.Password.Length < 8)
            errors.Add("Password must be at least 8 characters");
        if (!req.Password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter");
        if (!req.Password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter");
        if (!req.Password.Any(char.IsDigit))
            errors.Add("Password must contain at least one number");
        if (!req.Password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("Password must contain at least one special character");
        if (string.IsNullOrEmpty(req.Name) || req.Name.Length < 2)
            errors.Add("Name must be at least 2 characters");
        if (req.Role != Identity.Domain.Entities.UserRole.User && req.Role != Identity.Domain.Entities.UserRole.Admin)
            errors.Add("Invalid role. Must be 0 (User) or 1 (Admin)");

        if (errors.Any())
        {
            ThrowError(string.Join("; ", errors), 400);
            return;
        }

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
    public Identity.Domain.Entities.UserRole Role { get; init; } = Identity.Domain.Entities.UserRole.User;
}
