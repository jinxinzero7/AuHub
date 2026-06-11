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
        return User.Create("test@test.com", "stored_hash", "Test User", UserRole.User);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsAccessAndRefreshTokens()
    {
        var user = CreateTestUser();
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("password123", "stored_hash").Returns(true);
        _authService.GenerateJwtToken(user).Returns("jwt_token");
        _authService.GenerateRefreshToken().Returns("refresh_token");

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt_token");
        result.Value.RefreshToken.Should().Be("refresh_token");
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsUserData()
    {
        var user = CreateTestUser();
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("password123", "stored_hash").Returns(true);
        _authService.GenerateJwtToken(user).Returns("jwt_token");
        _authService.GenerateRefreshToken().Returns("refresh_token");

        var result = await _handler.HandleAsync(CreateCommand());

        result.Value.User.Id.Should().Be(user.Id);
        result.Value.User.Email.Should().Be("test@test.com");
        result.Value.User.Name.Should().Be("Test User");
        result.Value.User.Role.Should().Be("User");
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_CreatesRefreshTokenWithGeneratedValue()
    {
        var user = CreateTestUser();
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("password123", "stored_hash").Returns(true);
        _authService.GenerateJwtToken(Arg.Any<User>()).Returns("jwt_token");
        var refreshTokenValue = "generated_refresh_token_value";
        _authService.GenerateRefreshToken().Returns(refreshTokenValue);

        await _handler.HandleAsync(CreateCommand());

        await _refreshTokenRepo.Received(1).AddAsync(Arg.Is<RefreshToken>(rt =>
            rt.Token == refreshTokenValue &&
            rt.UserId == user.Id
        ), Arg.Any<CancellationToken>());
        await _refreshTokenRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_CallsVerifyPasswordWithRawPasswordAndStoredHash()
    {
        var command = CreateCommand();
        var user = CreateTestUser();
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _authService.GenerateJwtToken(Arg.Any<User>()).Returns("jwt_token");
        _authService.GenerateRefreshToken().Returns("refresh_token");

        await _handler.HandleAsync(command);

        _authService.Received(1).VerifyPassword(command.Password, user.PasswordHash);
    }

    [Fact]
    public async Task HandleAsync_WithValidCredentials_CallsGenerateJwtWithUserFromRepo()
    {
        var user = CreateTestUser();
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("password123", "stored_hash").Returns(true);
        _authService.GenerateJwtToken(Arg.Any<User>()).Returns("jwt_token");
        _authService.GenerateRefreshToken().Returns("refresh_token");

        await _handler.HandleAsync(CreateCommand());

        _authService.Received(1).GenerateJwtToken(Arg.Is<User>(u => u.Id == user.Id));
    }

    [Fact]
    public async Task HandleAsync_WithUnknownEmail_ReturnsFailure()
    {
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password");
        result.StatusCode.Should().Be(401);
        await _refreshTokenRepo.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithWrongPassword_ReturnsFailure()
    {
        var user = CreateTestUser();
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("password123", "stored_hash").Returns(false);

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password");
        result.StatusCode.Should().Be(401);
        await _refreshTokenRepo.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithBannedUser_ReturnsForbiddenWithoutCreatingRefreshToken()
    {
        var user = CreateTestUser();
        user.Ban("Policy violation");
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _authService.VerifyPassword("password123", "stored_hash").Returns(true);

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User is banned");
        result.StatusCode.Should().Be(403);
        _authService.DidNotReceive().GenerateJwtToken(Arg.Any<User>());
        await _refreshTokenRepo.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
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
