using FluentAssertions;
using Identity.Domain.Entities;

namespace Identity.UnitTests;

public class UserTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var user = User.Create("test@test.com", "hash", "Test User", UserRole.User);

        user.Email.Should().Be("test@test.com");
        user.PasswordHash.Should().Be("hash");
        user.Name.Should().Be("Test User");
        user.Role.Should().Be(UserRole.User);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.IsBanned.Should().BeFalse();
    }

    [Fact]
    public void Create_WithAdminRole_SetsRole()
    {
        var user = User.Create("admin@test.com", "hash", "Admin", UserRole.Admin);
        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void Create_NormalizesEmailToLower()
    {
        var user = User.Create("TEST@TEST.COM", "hash", "Test", UserRole.User);
        user.Email.Should().Be("test@test.com");
    }

    [Fact]
    public void UpdatePassword_UpdatesHash()
    {
        var user = User.Create("test@test.com", "oldhash", "Test", UserRole.User);
        user.UpdatePassword("newhash");

        user.PasswordHash.Should().Be("newhash");
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Ban_SetsBanned()
    {
        var user = User.Create("test@test.com", "hash", "Test", UserRole.User);
        user.Ban("Spam");

        user.IsBanned.Should().BeTrue();
        user.BannedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.BanReason.Should().Be("Spam");
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Unban_ClearsBanned()
    {
        var user = User.Create("test@test.com", "hash", "Test", UserRole.User);
        user.Ban("Spam");
        user.Unban();

        user.IsBanned.Should().BeFalse();
        user.BannedAt.Should().BeNull();
        user.BanReason.Should().BeNull();
    }

    [Fact]
    public void Unban_WithoutBan_DoesNotThrow()
    {
        var user = User.Create("test@test.com", "hash", "Test", UserRole.User);
        var act = () => user.Unban();
        act.Should().NotThrow();
    }
}
