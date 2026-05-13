using Identity.Domain.Entities;

namespace Identity.Application.Services;

public interface IAuthService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    string GenerateJwtToken(User user);
    string GenerateRefreshToken();
}
