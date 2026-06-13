using System.Security.Cryptography;
using System.Text;

namespace Identity.Application.Services;

public static class VerificationTokenService
{
    public static string GenerateToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    public static string GenerateNumericCode()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
