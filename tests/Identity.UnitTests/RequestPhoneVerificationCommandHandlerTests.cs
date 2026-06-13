using FluentAssertions;
using Identity.Application.Commands.Auth.PhoneVerification;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using NSubstitute;

namespace Identity.UnitTests;

public class RequestPhoneVerificationCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IPhoneVerificationCodeRepository _codeRepository;
    private readonly IPhoneVerificationSender _sender;
    private readonly RequestPhoneVerificationCommandHandler _handler;

    public RequestPhoneVerificationCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _codeRepository = Substitute.For<IPhoneVerificationCodeRepository>();
        _sender = Substitute.For<IPhoneVerificationSender>();
        _handler = new RequestPhoneVerificationCommandHandler(_userRepository, _codeRepository, _sender);
    }

    [Fact]
    public async Task HandleAsync_WithExistingUnverifiedUser_CreatesCodeAndSendsRawCode()
    {
        var user = User.Create("test@test.com", "+79990006000", "test_user", "hash", "Test User", UserRole.User);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        PhoneVerificationCode? savedCode = null;
        string? sentCode = null;
        await _codeRepository.AddAsync(Arg.Do<PhoneVerificationCode>(code => savedCode = code), Arg.Any<CancellationToken>());
        await _sender.SendAsync(user, Arg.Do<string>(code => sentCode = code), Arg.Any<CancellationToken>());

        var result = await _handler.HandleAsync(new RequestPhoneVerificationCommand { UserId = user.Id });

        result.IsSuccess.Should().BeTrue();
        sentCode.Should().MatchRegex("^\\d{6}$");
        savedCode.Should().NotBeNull();
        savedCode!.UserId.Should().Be(user.Id);
        savedCode.CodeHash.Should().Be(VerificationTokenService.HashToken(sentCode!));
        savedCode.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(9));
        await _codeRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _sender.Received(1).SendAsync(user, sentCode!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserAlreadyVerified_DoesNotCreateCode()
    {
        var user = User.Create("test@test.com", "+79990006001", "test_user", "hash", "Test User", UserRole.User);
        user.MarkPhoneVerified();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.HandleAsync(new RequestPhoneVerificationCommand { UserId = user.Id });

        result.IsSuccess.Should().BeTrue();
        await _codeRepository.DidNotReceive().AddAsync(Arg.Any<PhoneVerificationCode>(), Arg.Any<CancellationToken>());
        await _sender.DidNotReceive().SendAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.HandleAsync(new RequestPhoneVerificationCommand { UserId = userId });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        await _codeRepository.DidNotReceive().AddAsync(Arg.Any<PhoneVerificationCode>(), Arg.Any<CancellationToken>());
    }
}
