namespace Auctions.Application.Commands.Auth.RefreshToken;

public record RefreshTokenCommand
{
    public string RefreshToken { get; init; } = string.Empty;
}
