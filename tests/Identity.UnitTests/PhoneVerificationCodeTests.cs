using FluentAssertions;
using Identity.Domain.Entities;

namespace Identity.UnitTests;

public class PhoneVerificationCodeTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        var code = PhoneVerificationCode.Create(userId, "hash", expiresAt);

        code.Id.Should().NotBeEmpty();
        code.UserId.Should().Be(userId);
        code.CodeHash.Should().Be("hash");
        code.ExpiresAt.Should().Be(expiresAt);
        code.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        code.UsedAt.Should().BeNull();
    }

    [Fact]
    public void CanBeUsed_WhenActive_ReturnsTrue()
    {
        var code = PhoneVerificationCode.Create(Guid.NewGuid(), "hash", DateTime.UtcNow.AddMinutes(1));

        code.CanBeUsed(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void CanBeUsed_WhenExpired_ReturnsFalse()
    {
        var code = PhoneVerificationCode.Create(Guid.NewGuid(), "hash", DateTime.UtcNow.AddMinutes(-1));

        code.CanBeUsed(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void MarkUsed_SetsUsedAt()
    {
        var code = PhoneVerificationCode.Create(Guid.NewGuid(), "hash", DateTime.UtcNow.AddMinutes(1));

        code.MarkUsed();

        code.UsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        code.CanBeUsed(DateTime.UtcNow).Should().BeFalse();
    }
}
