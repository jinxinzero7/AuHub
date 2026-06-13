using AuHub.Shared.Results;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;

namespace Identity.Application.Commands.Auth.PhoneVerification;

public class RequestPhoneVerificationCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneVerificationCodeRepository _codeRepository;
    private readonly IPhoneVerificationSender _sender;

    public RequestPhoneVerificationCommandHandler(
        IUserRepository userRepository,
        IPhoneVerificationCodeRepository codeRepository,
        IPhoneVerificationSender sender)
    {
        _userRepository = userRepository;
        _codeRepository = codeRepository;
        _sender = sender;
    }

    public async Task<Result<RequestPhoneVerificationResponse>> HandleAsync(
        RequestPhoneVerificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<RequestPhoneVerificationResponse>("User not found", 404);
        }

        if (user.IsPhoneVerified)
        {
            return Result.Success(new RequestPhoneVerificationResponse { Success = true });
        }

        var code = VerificationTokenService.GenerateNumericCode();
        var codeHash = VerificationTokenService.HashToken(code);
        var verificationCode = PhoneVerificationCode.Create(
            user.Id,
            codeHash,
            DateTime.UtcNow.AddMinutes(10));

        await _codeRepository.AddAsync(verificationCode, cancellationToken);
        await _codeRepository.SaveChangesAsync(cancellationToken);
        await _sender.SendAsync(user, code, cancellationToken);

        return Result.Success(new RequestPhoneVerificationResponse { Success = true });
    }
}

public record RequestPhoneVerificationResponse
{
    public bool Success { get; init; }
}
