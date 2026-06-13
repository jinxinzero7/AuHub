using FluentAssertions;
using Identity.Domain.Entities;

namespace Identity.UnitTests;

public class EmailVerificationTokenTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        var token = EmailVerificationToken.Create(userId, "hash", expiresAt);

        token.Id.Should().NotBeEmpty();
        token.UserId.Should().Be(userId);
        token.TokenHash.Should().Be("hash");
        token.ExpiresAt.Should().Be(expiresAt);
        token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        token.UsedAt.Should().BeNull();
    }

    [Fact]
    public void CanBeUsed_WhenActive_ReturnsTrue()
    {
        var token = EmailVerificationToken.Create(Guid.NewGuid(), "hash", DateTime.UtcNow.AddMinutes(1));

        token.CanBeUsed(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void CanBeUsed_WhenExpired_ReturnsFalse()
    {
        var token = EmailVerificationToken.Create(Guid.NewGuid(), "hash", DateTime.UtcNow.AddMinutes(-1));

        token.CanBeUsed(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void MarkUsed_SetsUsedAt()
    {
        var token = EmailVerificationToken.Create(Guid.NewGuid(), "hash", DateTime.UtcNow.AddMinutes(1));

        token.MarkUsed();

        token.UsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        token.CanBeUsed(DateTime.UtcNow).Should().BeFalse();
    }
}
