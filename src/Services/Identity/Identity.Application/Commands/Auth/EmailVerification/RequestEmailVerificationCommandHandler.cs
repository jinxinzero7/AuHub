using AuHub.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;

namespace Identity.Application.Commands.Auth.EmailVerification;

public class RequestEmailVerificationCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IEmailVerificationSender _sender;

    public RequestEmailVerificationCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository tokenRepository,
        IEmailVerificationSender sender)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _sender = sender;
    }

    public async Task<Result<RequestEmailVerificationResponse>> HandleAsync(
        RequestEmailVerificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<RequestEmailVerificationResponse>("User not found", 404);
        }

        if (user.IsEmailVerified)
        {
            return Result.Success(new RequestEmailVerificationResponse { Success = true });
        }

        var token = VerificationTokenService.GenerateToken();
        var tokenHash = VerificationTokenService.HashToken(token);
        var verificationToken = EmailVerificationToken.Create(
            user.Id,
            tokenHash,
            DateTime.UtcNow.AddMinutes(30));

        await _tokenRepository.AddAsync(verificationToken, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);
        await _sender.SendAsync(user, token, cancellationToken);

        return Result.Success(new RequestEmailVerificationResponse { Success = true });
    }
}

public record RequestEmailVerificationResponse
{
    public bool Success { get; init; }
}
