using FluentAssertions;
using Identity.Application.Commands.Auth.EmailVerification;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using NSubstitute;

namespace Identity.UnitTests;

public class ConfirmEmailVerificationCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly ConfirmEmailVerificationCommandHandler _handler;

    public ConfirmEmailVerificationCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _tokenRepository = Substitute.For<IEmailVerificationTokenRepository>();
        _handler = new ConfirmEmailVerificationCommandHandler(_userRepository, _tokenRepository);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_VerifiesUserAndMarksTokenUsed()
    {
        var rawToken = "email-token";
        var tokenHash = VerificationTokenService.HashToken(rawToken);
        var user = User.Create("test@test.com", "+79990005000", "test_user", "hash", "Test User", UserRole.User);
        var verificationToken = EmailVerificationToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddMinutes(10));

        _tokenRepository.GetActiveByHashAsync(tokenHash, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(verificationToken);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.HandleAsync(new ConfirmEmailVerificationCommand { Token = rawToken });

        result.IsSuccess.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
        verificationToken.UsedAt.Should().NotBeNull();
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _tokenRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithMissingToken_ReturnsBadRequest()
    {
        var result = await _handler.HandleAsync(new ConfirmEmailVerificationCommand { Token = "" });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Verification token is required");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidToken_ReturnsBadRequest()
    {
        _tokenRepository.GetActiveByHashAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns((EmailVerificationToken?)null);

        var result = await _handler.HandleAsync(new ConfirmEmailVerificationCommand { Token = "invalid" });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Invalid or expired verification token");
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var rawToken = "email-token";
        var tokenHash = VerificationTokenService.HashToken(rawToken);
        var verificationToken = EmailVerificationToken.Create(Guid.NewGuid(), tokenHash, DateTime.UtcNow.AddMinutes(10));

        _tokenRepository.GetActiveByHashAsync(tokenHash, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(verificationToken);
        _userRepository.GetByIdAsync(verificationToken.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.HandleAsync(new ConfirmEmailVerificationCommand { Token = rawToken });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }
}
