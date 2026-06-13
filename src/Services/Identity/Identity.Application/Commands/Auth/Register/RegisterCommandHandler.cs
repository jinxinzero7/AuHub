using Identity.Application.Commands.Auth;
using Identity.Application.Services;
using Identity.Application.Mappings;
using AuHub.Shared.Results;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;

namespace Identity.Application.Commands.Auth.Register;

public class RegisterCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthService _authService;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuthService authService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _authService = authService;
    }

    public async Task<Result<RegisterResponse>> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existingUser = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
            if (existingUser != null)
            {
                return Result.Failure<RegisterResponse>("User with this email already exists", 400);
            }

            var existingPhoneUser = await _userRepository.GetByPhoneNumberAsync(command.PhoneNumber, cancellationToken);
            if (existingPhoneUser != null)
            {
                return Result.Failure<RegisterResponse>("User with this phone number already exists", 400);
            }

            var existingNicknameUser = await _userRepository.GetByNicknameAsync(command.Nickname, cancellationToken);
            if (existingNicknameUser != null)
            {
                return Result.Failure<RegisterResponse>("User with this nickname already exists", 400);
            }

            var passwordHash = _authService.HashPassword(command.Password);

            var user = User.Create(
                command.Email,
                command.PhoneNumber,
                command.Nickname,
                passwordHash,
                command.Name,
                UserRole.User);

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            var accessToken = _authService.GenerateJwtToken(user);
            var refreshTokenValue = _authService.GenerateRefreshToken();

            var refreshToken = Identity.Domain.Entities.RefreshToken.Create(
                user.Id,
                refreshTokenValue,
                DateTime.UtcNow.AddDays(30)
            );

            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            var response = new RegisterResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                User = user.ToDto()
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            return Result.Failure<RegisterResponse>($"Failed to register user: {ex.Message}", 500);
        }
    }
}

public record RegisterResponse
{
    public bool Success { get; init; }
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public UserDto User { get; init; } = null!;
}
