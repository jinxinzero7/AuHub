namespace Identity.Application.Commands.Auth;

public record UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Nickname { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsEmailVerified { get; init; }
    public bool IsPhoneVerified { get; init; }
    public string DocumentVerificationStatus { get; init; } = string.Empty;
}
