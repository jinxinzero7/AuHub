using FluentAssertions;
using Identity.Domain.Entities;

namespace Identity.UnitTests;

public class RefreshTokenTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_SetsProperties()
    {
        var token = RefreshToken.Create(UserId, "token-value", DateTime.UtcNow.AddDays(30));

        token.UserId.Should().Be(UserId);
        token.Token.Should().Be("token-value");
        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(1));
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void Create_WithoutFamilyId_GeneratesNewFamilyId()
    {
        var token = RefreshToken.Create(UserId, "token-value", DateTime.UtcNow.AddDays(30));
        token.FamilyId.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithFamilyId_UsesProvided()
    {
        var familyId = Guid.NewGuid();
        var token = RefreshToken.Create(UserId, "token-value", DateTime.UtcNow.AddDays(30), familyId);
        token.FamilyId.Should().Be(familyId);
    }

    [Fact]
    public void Revoke_SetsRevoked()
    {
        var token = RefreshToken.Create(UserId, "token-value", DateTime.UtcNow.AddDays(30));
        token.Revoke();

        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ReplaceBy_RevokesAndSetsReplacement()
    {
        var token = RefreshToken.Create(UserId, "token-value", DateTime.UtcNow.AddDays(30));
        var newId = Guid.NewGuid();
        token.ReplaceBy(newId);

        token.IsRevoked.Should().BeTrue();
        token.ReplacedByTokenId.Should().Be(newId);
    }

    [Fact]
    public void IsValid_NotRevokedAndNotExpired_ReturnsTrue()
    {
        var token = RefreshToken.Create(UserId, "token-value", DateTime.UtcNow.AddDays(30));
        token.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenExpired_ReturnsFalse()
    {
        var token = RefreshToken.Create(UserId, "token-value", DateTime.UtcNow.AddDays(-1));
        token.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenRevoked_ReturnsFalse()
    {
        var token = RefreshToken.Create(UserId, "token-value", DateTime.UtcNow.AddDays(30));
        token.Revoke();
        token.IsValid().Should().BeFalse();
    }
}
