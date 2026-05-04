using Auctions.Application.Commands.Auth;
using Auctions.Application.Services;
using Auctions.Domain.Common;
using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Commands.Auth.Register;

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
            // Проверка существования пользователя
            var existingUser = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
            if (existingUser != null)
            {
                return Result.Failure<RegisterResponse>("User with this email already exists", 400);
            }

            // Хеширование пароля
            var passwordHash = _authService.HashPassword(command.Password);

            // Создание пользователя
            var user = User.Create(command.Email, passwordHash, command.Name, command.Role);

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

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

            var response = new RegisterResponse
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
