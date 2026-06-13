using AuHub.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Interfaces;

namespace Identity.Application.Commands.Auth.EmailVerification;

public class ConfirmEmailVerificationCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;

    public ConfirmEmailVerificationCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
    }

    public async Task<Result<ConfirmEmailVerificationResponse>> HandleAsync(
        ConfirmEmailVerificationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result.Failure<ConfirmEmailVerificationResponse>("Verification token is required", 400);
        }

        var tokenHash = VerificationTokenService.HashToken(command.Token);
        var verificationToken = await _tokenRepository.GetActiveByHashAsync(
            tokenHash,
            DateTime.UtcNow,
            cancellationToken);

        if (verificationToken == null)
        {
            return Result.Failure<ConfirmEmailVerificationResponse>("Invalid or expired verification token", 400);
        }

        var user = await _userRepository.GetByIdAsync(verificationToken.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<ConfirmEmailVerificationResponse>("User not found", 404);
        }

        user.MarkEmailVerified();
        verificationToken.MarkUsed();

        await _userRepository.SaveChangesAsync(cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new ConfirmEmailVerificationResponse { Success = true });
    }
}

public record ConfirmEmailVerificationResponse
{
    public bool Success { get; init; }
}
