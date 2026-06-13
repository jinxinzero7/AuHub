namespace Identity.Application.Commands.Auth.EmailVerification;

public record RequestEmailVerificationCommand
{
    public Guid UserId { get; init; }
}
