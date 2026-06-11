using AuHub.Shared.Results;
using FluentAssertions;
using Identity.Application.Commands.Auth.RefreshToken;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Identity.UnitTests;

public class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IAuthService _authService;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();
        _authService = Substitute.For<IAuthService>();
        _handler = new RefreshTokenCommandHandler(_refreshTokenRepo, _authService);
    }

    private RefreshTokenCommand CreateCommand(string token = "valid-token")
    {
        return new RefreshTokenCommand { RefreshToken = token };
    }

    private RefreshToken CreateValidToken(Guid? familyId = null)
    {
        return RefreshToken.Create(Guid.NewGuid(), "valid-token", DateTime.UtcNow.AddDays(30), familyId);
    }

    private static void AttachUser(RefreshToken token, User user)
    {
        typeof(RefreshToken)
            .GetProperty(nameof(RefreshToken.User))!
            .SetValue(token, user);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ReturnsNewAccessAndRefreshTokens()
    {
        var token = CreateValidToken();
        _refreshTokenRepo.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(token);
        _authService.GenerateJwtToken(Arg.Any<User>()).Returns("new_jwt");
        _authService.GenerateRefreshToken().Returns("new_refresh");

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new_jwt");
        result.Value.RefreshToken.Should().Be("new_refresh");
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_RevokesOldToken()
    {
        var token = CreateValidToken();
        _refreshTokenRepo.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(token);
        _authService.GenerateJwtToken(Arg.Any<User>()).Returns("new_jwt");
        _authService.GenerateRefreshToken().Returns("new_refresh");

        await _handler.HandleAsync(CreateCommand());

        token.IsRevoked.Should().BeTrue();
        token.ReplacedByTokenId.Should().NotBeNull();
        await _refreshTokenRepo.Received(1).UpdateAsync(token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_CreatesNewTokenWithSameFamilyId()
    {
        var familyId = Guid.NewGuid();
        var token = CreateValidToken(familyId);
        _refreshTokenRepo.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(token);
        _authService.GenerateJwtToken(Arg.Any<User>()).Returns("new_jwt");
        var newRefreshValue = "generated_new_refresh";
        _authService.GenerateRefreshToken().Returns(newRefreshValue);

        await _handler.HandleAsync(CreateCommand());

        await _refreshTokenRepo.Received(1).AddAsync(Arg.Is<RefreshToken>(t =>
            t.FamilyId == familyId &&
            t.Token == newRefreshValue &&
            t.UserId == token.UserId &&
            t.IsRevoked == false
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_CallsUpdateBeforeAddBeforeSave()
    {
        var token = CreateValidToken();
        _refreshTokenRepo.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(token);
        _authService.GenerateJwtToken(Arg.Any<User>()).Returns("new_jwt");
        _authService.GenerateRefreshToken().Returns("new_refresh");

        await _handler.HandleAsync(CreateCommand());

        Received.InOrder(() =>
        {
            _refreshTokenRepo.UpdateAsync(token, Arg.Any<CancellationToken>());
            _refreshTokenRepo.AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
            _refreshTokenRepo.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task HandleAsync_WithInvalidToken_ReturnsFailure()
    {
        _refreshTokenRepo.GetByTokenAsync("invalid-token", Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        var result = await _handler.HandleAsync(CreateCommand("invalid-token"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid or expired refresh token");
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task HandleAsync_WithRevokedTokenAndFamilyId_RevokesFamilyAndReturnsFailure()
    {
        var familyId = Guid.NewGuid();
        var token = CreateValidToken(familyId);
        token.Revoke();
        _refreshTokenRepo.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(token);

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Refresh token reuse detected");
        result.StatusCode.Should().Be(401);
        await _refreshTokenRepo.Received(1).RevokeFamilyAsync(familyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithExpiredToken_ReturnsFailure()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "expired-token", DateTime.UtcNow.AddDays(-1));
        _refreshTokenRepo.GetByTokenAsync("expired-token", Arg.Any<CancellationToken>()).Returns(token);

        var result = await _handler.HandleAsync(CreateCommand("expired-token"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Refresh token expired");
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task HandleAsync_WithBannedUser_ReturnsForbiddenAndRevokesTokenFamily()
    {
        var familyId = Guid.NewGuid();
        var user = User.Create("banned@test.com", "hash", "Banned User", UserRole.User);
        user.Ban("Policy violation");
        var token = CreateValidToken(familyId);
        AttachUser(token, user);
        _refreshTokenRepo.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(token);

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User is banned");
        result.StatusCode.Should().Be(403);
        await _refreshTokenRepo.Received(1).RevokeFamilyAsync(familyId, Arg.Any<CancellationToken>());
        await _refreshTokenRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _authService.DidNotReceive().GenerateJwtToken(Arg.Any<User>());
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        var token = CreateValidToken();
        _refreshTokenRepo.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(token);
        _authService.GenerateJwtToken(Arg.Any<User>()).Returns("new_jwt");
        _authService.GenerateRefreshToken().Returns("new_refresh");
        _refreshTokenRepo.When(x => x.UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new Exception("DB error"));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to refresh token");
        result.StatusCode.Should().Be(500);
    }
}
