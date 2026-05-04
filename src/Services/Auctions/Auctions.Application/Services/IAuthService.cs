using Auctions.Domain.Entities;

namespace Auctions.Application.Services;

public interface IAuthService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    string GenerateJwtToken(User user);
    string GenerateRefreshToken();
}
