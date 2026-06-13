namespace Identity.Application.Commands.Auth.EmailVerification;

public record ConfirmEmailVerificationCommand
{
    public string Token { get; init; } = string.Empty;
}
