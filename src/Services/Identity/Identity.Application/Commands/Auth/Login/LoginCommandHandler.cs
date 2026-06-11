using Identity.Application.Commands.Auth;
using Identity.Application.Services;
using Identity.Application.Mappings;
using AuHub.Shared.Results;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;

namespace Identity.Application.Commands.Auth.Login;

public class LoginCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthService _authService;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuthService authService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _authService = authService;
    }

    public async Task<Result<LoginResponse>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);

            if (user == null)
            {
                return Result.Failure<LoginResponse>("Invalid email or password", 401);
            }

            if (!_authService.VerifyPassword(command.Password, user.PasswordHash))
            {
                return Result.Failure<LoginResponse>("Invalid email or password", 401);
            }

            if (user.IsBanned)
            {
                return Result.Failure<LoginResponse>("User is banned", 403);
            }

            var accessToken = _authService.GenerateJwtToken(user);
            var refreshTokenValue = _authService.GenerateRefreshToken();

            var refreshToken = Identity.Domain.Entities.RefreshToken.Create(
                user.Id,
                refreshTokenValue,
                DateTime.UtcNow.AddDays(30)
            );

            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            var response = new LoginResponse
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
            return Result.Failure<LoginResponse>($"Failed to login: {ex.Message}", 500);
        }
    }
}

public record LoginResponse
{
    public bool Success { get; init; }
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public UserDto User { get; init; } = null!;
}
