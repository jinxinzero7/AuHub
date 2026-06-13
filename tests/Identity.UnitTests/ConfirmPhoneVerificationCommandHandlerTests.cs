using FluentAssertions;
using Identity.Application.Commands.Auth.PhoneVerification;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using NSubstitute;

namespace Identity.UnitTests;

public class ConfirmPhoneVerificationCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneVerificationCodeRepository _codeRepository;
    private readonly ConfirmPhoneVerificationCommandHandler _handler;

    public ConfirmPhoneVerificationCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _codeRepository = Substitute.For<IPhoneVerificationCodeRepository>();
        _handler = new ConfirmPhoneVerificationCommandHandler(_userRepository, _codeRepository);
    }

    [Fact]
    public async Task HandleAsync_WithValidCode_VerifiesUserAndMarksCodeUsed()
    {
        var rawCode = "123456";
        var codeHash = VerificationTokenService.HashToken(rawCode);
        var user = User.Create("test@test.com", "+79990007000", "test_user", "hash", "Test User", UserRole.User);
        var verificationCode = PhoneVerificationCode.Create(user.Id, codeHash, DateTime.UtcNow.AddMinutes(10));

        _codeRepository.GetActiveByUserIdAndHashAsync(user.Id, codeHash, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(verificationCode);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.HandleAsync(
            new ConfirmPhoneVerificationCommand
            {
                UserId = user.Id,
                Code = rawCode
            });

        result.IsSuccess.Should().BeTrue();
        user.IsPhoneVerified.Should().BeTrue();
        verificationCode.UsedAt.Should().NotBeNull();
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _codeRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    public async Task HandleAsync_WithInvalidCodeFormat_ReturnsBadRequest(string code)
    {
        var result = await _handler.HandleAsync(
            new ConfirmPhoneVerificationCommand
            {
                UserId = Guid.NewGuid(),
                Code = code
            });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Verification code must contain 6 digits");
        await _codeRepository.DidNotReceive()
            .GetActiveByUserIdAndHashAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithUnknownCode_ReturnsBadRequest()
    {
        _codeRepository.GetActiveByUserIdAndHashAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns((PhoneVerificationCode?)null);

        var result = await _handler.HandleAsync(
            new ConfirmPhoneVerificationCommand
            {
                UserId = Guid.NewGuid(),
                Code = "123456"
            });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Invalid or expired verification code");
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var rawCode = "123456";
        var userId = Guid.NewGuid();
        var codeHash = VerificationTokenService.HashToken(rawCode);
        var verificationCode = PhoneVerificationCode.Create(userId, codeHash, DateTime.UtcNow.AddMinutes(10));

        _codeRepository.GetActiveByUserIdAndHashAsync(userId, codeHash, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(verificationCode);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.HandleAsync(
            new ConfirmPhoneVerificationCommand
            {
                UserId = userId,
                Code = rawCode
            });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }
}
