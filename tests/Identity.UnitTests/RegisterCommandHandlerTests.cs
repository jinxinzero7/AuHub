using AuHub.Shared.Results;
using FluentAssertions;
using Identity.Application.Commands.Auth.Register;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Identity.UnitTests;

public class RegisterCommandHandlerTests
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IAuthService _authService;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
        _authService = Substitute.For<IAuthService>();
        _handler = new RegisterCommandHandler(_userRepo, _refreshTokenRepo, _authService);
    }

    private RegisterCommand CreateCommand(string email = "test@test.com")
    {
        return new RegisterCommand
        {
            Email = email,
            Password = "password123",
            Name = "Test User",
            Role = UserRole.User
        };
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ReturnsSuccess()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _authService.HashPassword(Arg.Any<string>()).Returns("hashed_password");
        _authService.GenerateJwtToken(Arg.Any<User>()).Returns("jwt_token");
        _authService.GenerateRefreshToken().Returns("refresh_token");

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt_token");
        result.Value.RefreshToken.Should().Be("refresh_token");
    }

    [Fact]
    public async Task HandleAsync_WithValidData_CreatesUserAndToken()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _authService.HashPassword(Arg.Any<string>()).Returns("hashed_password");
        _authService.GenerateJwtToken(Arg.Any<User>()).Returns("jwt_token");
        _authService.GenerateRefreshToken().Returns("refresh_token");

        await _handler.HandleAsync(CreateCommand());

        await _userRepo.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Email == "test@test.com" &&
            u.PasswordHash == "hashed_password" &&
            u.Name == "Test User" &&
            u.Role == UserRole.User), Arg.Any<CancellationToken>());
        await _refreshTokenRepo.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _userRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokenRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmail_ReturnsFailure()
    {
        var existingUser = User.Create("test@test.com", "hash", "Existing", UserRole.User);
        _userRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(existingUser);

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User with this email already exists");
        result.StatusCode.Should().Be(400);
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _authService.HashPassword(Arg.Any<string>()).Returns("hashed_password");
        _authService.GenerateJwtToken(Arg.Any<User>()).Returns("jwt_token");
        _authService.GenerateRefreshToken().Returns("refresh_token");
        _userRepo.When(x => x.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new Exception("DB error"));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to register user");
        result.StatusCode.Should().Be(500);
    }
}
