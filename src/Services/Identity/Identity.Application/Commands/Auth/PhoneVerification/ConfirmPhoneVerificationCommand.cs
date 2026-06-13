namespace Identity.Application.Commands.Auth.PhoneVerification;

public record ConfirmPhoneVerificationCommand
{
    public Guid UserId { get; init; }
    public string Code { get; init; } = string.Empty;
}
