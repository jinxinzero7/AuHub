using FluentAssertions;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Identity.UnitTests;

public class AuthServiceTests
{
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "TestSecretKeyThatIsAtLeast32BytesLongForSecurity!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        _authService = new AuthService(config);
    }

    [Fact]
    public void HashPassword_ReturnsHashedString()
    {
        var hash = _authService.HashPassword("password123");

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("password123");
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashesForSamePassword()
    {
        var hash1 = _authService.HashPassword("password123");
        var hash2 = _authService.HashPassword("password123");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var hash = _authService.HashPassword("password123");
        var result = _authService.VerifyPassword("password123", hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = _authService.HashPassword("password123");
        var result = _authService.VerifyPassword("wrongpassword", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
    {
        var hash = _authService.HashPassword("password123");
        var result = _authService.VerifyPassword("", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        var token = _authService.GenerateRefreshToken();

        token.Should().NotBeNullOrEmpty();
        var bytes = Convert.FromBase64String(token);
        bytes.Should().HaveCount(64);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        var token1 = _authService.GenerateRefreshToken();
        var token2 = _authService.GenerateRefreshToken();

        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateJwtToken_ReturnsValidToken()
    {
        var user = User.Create("test@test.com", "+79990002000", "test_user", "hash", "Test User", UserRole.User);
        var token = _authService.GenerateJwtToken(user);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateJwtToken_WithAdminRole_ContainsRoleClaim()
    {
        var user = User.Create("admin@test.com", "+79990002001", "admin_user", "hash", "Admin", UserRole.Admin);
        var token = _authService.GenerateJwtToken(user);

        token.Should().NotBeNullOrEmpty();
        var payload = token.Split('.')[1];
        var decoded = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=')));
        decoded.Should().Contain("Admin");
    }
}
