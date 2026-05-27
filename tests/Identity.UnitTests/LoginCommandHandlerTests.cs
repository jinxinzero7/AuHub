using AuHub.Shared.Results;
using FluentAssertions;
using Identity.Application.Commands.Auth.Login;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Identity.UnitTests;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IAuthService _authService;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
        _authService = Substitute.For<IAuthService>();
        _handler = new LoginCommandHandler(_userRepo, _refreshTokenRepo, _authService);
    }

    private LoginCommand CreateCommand()
    {
        return new LoginCommand
        {
            Email = "test@test.com",
            Password = "password123"
        };
    }

    private User CreateTestUser()
    {
        return User.Create("test@test.com", "hashed_password", "Test User", UserRole.User);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsSuccess()
    {
        var user = CreateTestUser();
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("password123", "hashed_password").Returns(true);
        _authService.GenerateJwtToken(user).Returns("jwt_token");
        _authService.GenerateRefreshToken().Returns("refresh_token");

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt_token");
        result.Value.RefreshToken.Should().Be("refresh_token");
        result.Value.User.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_CreatesRefreshToken()
    {
        var user = CreateTestUser();
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("password123", "hashed_password").Returns(true);
        _authService.GenerateJwtToken(user).Returns("jwt_token");
        _authService.GenerateRefreshToken().Returns("refresh_token");

        await _handler.HandleAsync(CreateCommand());

        await _refreshTokenRepo.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _refreshTokenRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithUnknownEmail_ReturnsFailure()
    {
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password");
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task HandleAsync_WithWrongPassword_ReturnsFailure()
    {
        var user = CreateTestUser();
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("password123", "hashed_password").Returns(false);

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password");
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB error"));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to login");
        result.StatusCode.Should().Be(500);
    }
}
