namespace Identity.Application.Commands.Auth.PhoneVerification;

public record RequestPhoneVerificationCommand
{
    public Guid UserId { get; init; }
}
