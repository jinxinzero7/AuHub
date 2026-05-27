using AuHub.Shared.Results;
using FluentAssertions;
using Notifications.Application.Queries.GetUnreadCount;
using Notifications.Application.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Notifications.UnitTests;

public class GetUnreadCountQueryHandlerTests
{
    private readonly INotificationRepository _repository;
    private readonly GetUnreadCountQueryHandler _handler;

    public GetUnreadCountQueryHandlerTests()
    {
        _repository = Substitute.For<INotificationRepository>();
        _handler = new GetUnreadCountQueryHandler(_repository);
    }

    private GetUnreadCountQuery CreateQuery(Guid? userId = null)
    {
        return new GetUnreadCountQuery { UserId = userId ?? Guid.NewGuid() };
    }

    [Fact]
    public async Task HandleAsync_ReturnsCount()
    {
        var userId = Guid.NewGuid();
        _repository.CountByUserIdAsync(userId, true, Arg.Any<CancellationToken>()).Returns(5);

        var result = await _handler.HandleAsync(CreateQuery(userId));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }

    [Fact]
    public async Task HandleAsync_WithNoUnread_ReturnsZero()
    {
        var userId = Guid.NewGuid();
        _repository.CountByUserIdAsync(userId, true, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _handler.HandleAsync(CreateQuery(userId));

        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_CallsRepositoryWithOnlyUnreadTrue()
    {
        var userId = Guid.NewGuid();
        _repository.CountByUserIdAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(0);

        await _handler.HandleAsync(CreateQuery(userId));

        await _repository.Received(1).CountByUserIdAsync(userId, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _repository.CountByUserIdAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB error"));

        var result = await _handler.HandleAsync(CreateQuery());

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(500);
    }
}
