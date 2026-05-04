using Auctions.Application.Commands.Auth;
using Auctions.Application.Services;
using Auctions.Domain.Common;
using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Commands.Auth.Login;

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

            // Генерация токенов
            var accessToken = _authService.GenerateJwtToken(user);
            var refreshTokenValue = _authService.GenerateRefreshToken();

            // Создание refresh token
            var refreshToken = Domain.Entities.RefreshToken.Create(
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
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role.ToString()
                }
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
