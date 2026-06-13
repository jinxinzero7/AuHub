using FluentAssertions;
using Identity.Domain.Entities;

namespace Identity.UnitTests;

public class UserTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var user = User.Create("test@test.com", "+79990000000", "test_user", "hash", "Test User", UserRole.User);

        user.Email.Should().Be("test@test.com");
        user.PhoneNumber.Should().Be("+79990000000");
        user.Nickname.Should().Be("test_user");
        user.PasswordHash.Should().Be("hash");
        user.Name.Should().Be("Test User");
        user.Role.Should().Be(UserRole.User);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.IsEmailVerified.Should().BeFalse();
        user.IsPhoneVerified.Should().BeFalse();
        user.IsBanned.Should().BeFalse();
    }

    [Fact]
    public void Create_WithAdminRole_SetsRole()
    {
        var user = User.Create("admin@test.com", "+79990000001", "admin_user", "hash", "Admin", UserRole.Admin);
        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void Create_NormalizesEmailToLower()
    {
        var user = User.Create("TEST@TEST.COM", "+79990000002", "test_user", "hash", "Test", UserRole.User);
        user.Email.Should().Be("test@test.com");
    }

    [Fact]
    public void Create_NormalizesPhoneNumber()
    {
        var user = User.Create("test@test.com", "+7 (999) 000-00-03", "test_user", "hash", "Test", UserRole.User);
        user.PhoneNumber.Should().Be("+79990000003");
    }

    [Fact]
    public void MarkEmailVerified_SetsVerificationState()
    {
        var user = User.Create("test@test.com", "+79990000004", "test_user", "hash", "Test", UserRole.User);

        user.MarkEmailVerified();

        user.IsEmailVerified.Should().BeTrue();
        user.EmailVerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkPhoneVerified_SetsVerificationState()
    {
        var user = User.Create("test@test.com", "+79990000005", "test_user", "hash", "Test", UserRole.User);

        user.MarkPhoneVerified();

        user.IsPhoneVerified.Should().BeTrue();
        user.PhoneVerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdatePassword_UpdatesHash()
    {
        var user = User.Create("test@test.com", "+79990000006", "test_user", "oldhash", "Test", UserRole.User);
        user.UpdatePassword("newhash");

        user.PasswordHash.Should().Be("newhash");
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Ban_SetsBanned()
    {
        var user = User.Create("test@test.com", "+79990000007", "test_user", "hash", "Test", UserRole.User);
        user.Ban("Spam");

        user.IsBanned.Should().BeTrue();
        user.BannedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.BanReason.Should().Be("Spam");
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Unban_ClearsBanned()
    {
        var user = User.Create("test@test.com", "+79990000008", "test_user", "hash", "Test", UserRole.User);
        user.Ban("Spam");
        user.Unban();

        user.IsBanned.Should().BeFalse();
        user.BannedAt.Should().BeNull();
        user.BanReason.Should().BeNull();
    }

    [Fact]
    public void Unban_WithoutBan_DoesNotThrow()
    {
        var user = User.Create("test@test.com", "+79990000009", "test_user", "hash", "Test", UserRole.User);
        var act = () => user.Unban();
        act.Should().NotThrow();
    }
}
