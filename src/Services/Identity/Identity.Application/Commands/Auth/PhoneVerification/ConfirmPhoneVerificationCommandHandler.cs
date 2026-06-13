using AuHub.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Interfaces;

namespace Identity.Application.Commands.Auth.PhoneVerification;

public partial class ConfirmPhoneVerificationCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneVerificationCodeRepository _codeRepository;

    public ConfirmPhoneVerificationCommandHandler(
        IUserRepository userRepository,
        IPhoneVerificationCodeRepository codeRepository)
    {
        _userRepository = userRepository;
        _codeRepository = codeRepository;
    }

    public async Task<Result<ConfirmPhoneVerificationResponse>> HandleAsync(
        ConfirmPhoneVerificationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!SixDigitCodeRegex().IsMatch(command.Code))
        {
            return Result.Failure<ConfirmPhoneVerificationResponse>("Verification code must contain 6 digits", 400);
        }

        var codeHash = VerificationTokenService.HashToken(command.Code);
        var verificationCode = await _codeRepository.GetActiveByUserIdAndHashAsync(
            command.UserId,
            codeHash,
            DateTime.UtcNow,
            cancellationToken);

        if (verificationCode == null)
        {
            return Result.Failure<ConfirmPhoneVerificationResponse>("Invalid or expired verification code", 400);
        }

        var user = await _userRepository.GetByIdAsync(verificationCode.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<ConfirmPhoneVerificationResponse>("User not found", 404);
        }

        user.MarkPhoneVerified();
        verificationCode.MarkUsed();

        await _userRepository.SaveChangesAsync(cancellationToken);
        await _codeRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new ConfirmPhoneVerificationResponse { Success = true });
    }

    [System.Text.RegularExpressions.GeneratedRegex("^\\d{6}$")]
    private static partial System.Text.RegularExpressions.Regex SixDigitCodeRegex();
}

public record ConfirmPhoneVerificationResponse
{
    public bool Success { get; init; }
}
