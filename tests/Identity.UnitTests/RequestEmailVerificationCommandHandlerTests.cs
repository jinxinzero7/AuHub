using FluentAssertions;
using Identity.Application.Commands.Auth.EmailVerification;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using NSubstitute;

namespace Identity.UnitTests;

public class RequestEmailVerificationCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IEmailVerificationSender _sender;
    private readonly RequestEmailVerificationCommandHandler _handler;

    public RequestEmailVerificationCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _tokenRepository = Substitute.For<IEmailVerificationTokenRepository>();
        _sender = Substitute.For<IEmailVerificationSender>();
        _handler = new RequestEmailVerificationCommandHandler(_userRepository, _tokenRepository, _sender);
    }

    [Fact]
    public async Task HandleAsync_WithExistingUnverifiedUser_CreatesTokenAndSendsRawToken()
    {
        var user = User.Create("test@test.com", "+79990004000", "test_user", "hash", "Test User", UserRole.User);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        EmailVerificationToken? savedToken = null;
        string? sentToken = null;
        await _tokenRepository.AddAsync(Arg.Do<EmailVerificationToken>(token => savedToken = token), Arg.Any<CancellationToken>());
        await _sender.SendAsync(user, Arg.Do<string>(token => sentToken = token), Arg.Any<CancellationToken>());

        var result = await _handler.HandleAsync(new RequestEmailVerificationCommand { UserId = user.Id });

        result.IsSuccess.Should().BeTrue();
        savedToken.Should().NotBeNull();
        sentToken.Should().NotBeNullOrWhiteSpace();
        savedToken!.UserId.Should().Be(user.Id);
        savedToken.TokenHash.Should().Be(VerificationTokenService.HashToken(sentToken!));
        savedToken.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(29));
        await _tokenRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _sender.Received(1).SendAsync(user, sentToken!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserAlreadyVerified_DoesNotCreateToken()
    {
        var user = User.Create("test@test.com", "+79990004001", "test_user", "hash", "Test User", UserRole.User);
        user.MarkEmailVerified();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.HandleAsync(new RequestEmailVerificationCommand { UserId = user.Id });

        result.IsSuccess.Should().BeTrue();
        await _tokenRepository.DidNotReceive().AddAsync(Arg.Any<EmailVerificationToken>(), Arg.Any<CancellationToken>());
        await _sender.DidNotReceive().SendAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.HandleAsync(new RequestEmailVerificationCommand { UserId = userId });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        await _tokenRepository.DidNotReceive().AddAsync(Arg.Any<EmailVerificationToken>(), Arg.Any<CancellationToken>());
    }
}
