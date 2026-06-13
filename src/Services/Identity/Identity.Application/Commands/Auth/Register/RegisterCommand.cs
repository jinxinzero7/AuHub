using Identity.Domain.Entities;

namespace Identity.Application.Commands.Auth.Register;

public record RegisterCommand
{
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Nickname { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public UserRole Role { get; init; } = UserRole.User;
}
