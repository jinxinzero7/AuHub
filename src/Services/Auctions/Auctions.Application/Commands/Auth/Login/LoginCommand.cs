namespace Auctions.Application.Commands.Auth.Login;

public record LoginCommand
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
