using Identity.Application.Services;
using AuHub.Shared.Results;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;

namespace Identity.Application.Commands.Auth.RefreshToken;

public class RefreshTokenCommandHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthService _authService;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IAuthService authService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _authService = authService;
    }

    public async Task<Result<RefreshTokenResponse>> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(command.RefreshToken, cancellationToken);

            if (refreshToken == null || !refreshToken.IsValid())
            {
                return Result.Failure<RefreshTokenResponse>("Invalid or expired refresh token", 401);
            }

            // Revoke старый токен (удаление из БД для ротации)
            await _refreshTokenRepository.RevokeTokenAsync(refreshToken.Id, cancellationToken);

            // Генерация новых токенов
            var accessToken = _authService.GenerateJwtToken(refreshToken.User);
            var newRefreshTokenValue = _authService.GenerateRefreshToken();

            // Создание нового refresh token
            var newRefreshToken = Identity.Domain.Entities.RefreshToken.Create(
                refreshToken.UserId,
                newRefreshTokenValue,
                DateTime.UtcNow.AddDays(30)
            );

            await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

            var response = new RefreshTokenResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = newRefreshTokenValue
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            return Result.Failure<RefreshTokenResponse>($"Failed to refresh token: {ex.Message}", 500);
        }
    }
}

public record RefreshTokenResponse
{
    public bool Success { get; init; }
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}
