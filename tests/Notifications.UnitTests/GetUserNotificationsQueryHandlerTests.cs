using AuHub.Shared.Results;
using FluentAssertions;
using Notifications.Application.Queries.GetUserNotifications;
using Notifications.Application.Repositories;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Notifications.UnitTests;

public class GetUserNotificationsQueryHandlerTests
{
    private readonly INotificationRepository _repository;
    private readonly GetUserNotificationsQueryHandler _handler;

    public GetUserNotificationsQueryHandlerTests()
    {
        _repository = Substitute.For<INotificationRepository>();
        _handler = new GetUserNotificationsQueryHandler(_repository);
    }

    private GetUserNotificationsQuery CreateQuery(Guid? userId = null)
    {
        return new GetUserNotificationsQuery
        {
            UserId = userId ?? Guid.NewGuid(),
            Page = 1,
            PageSize = 20,
            OnlyUnread = false
        };
    }

    private List<Notification> CreateNotifications(Guid userId, int count)
    {
        return Enumerable.Range(1, count).Select(i =>
            Notification.Create(userId, NotificationType.NewBid, $"Title {i}", $"Message {i}")
        ).ToList();
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaginatedNotifications()
    {
        var userId = Guid.NewGuid();
        var notifications = CreateNotifications(userId, 3);
        var query = CreateQuery(userId);
        _repository.GetByUserIdAsync(userId, 1, 20, false, Arg.Any<CancellationToken>()).Returns(notifications);
        _repository.CountByUserIdAsync(userId, false, Arg.Any<CancellationToken>()).Returns(15);

        var result = await _handler.HandleAsync(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Notifications.Should().HaveCount(3);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalCount.Should().Be(15);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_WithTotalCountCalculatesTotalPages()
    {
        var userId = Guid.NewGuid();
        var notifications = CreateNotifications(userId, 10);
        var query = CreateQuery(userId);
        query.PageSize = 5;
        _repository.GetByUserIdAsync(userId, 1, 5, false, Arg.Any<CancellationToken>()).Returns(notifications);
        _repository.CountByUserIdAsync(userId, false, Arg.Any<CancellationToken>()).Returns(25);

        var result = await _handler.HandleAsync(query);

        result.Value.TotalPages.Should().Be(5);
    }

    [Fact]
    public async Task HandleAsync_MapsNotificationToDto()
    {
        var userId = Guid.NewGuid();
        var notification = Notification.Create(userId, NotificationType.WonAuction, "You won!", "Final price $500");
        _repository.GetByUserIdAsync(userId, 1, 20, false, Arg.Any<CancellationToken>())
            .Returns(new List<Notification> { notification });
        _repository.CountByUserIdAsync(userId, false, Arg.Any<CancellationToken>()).Returns(1);
        var query = CreateQuery(userId);

        var result = await _handler.HandleAsync(query);

        var dto = result.Value.Notifications.Single();
        dto.Id.Should().Be(notification.Id);
        dto.Type.Should().Be((int)NotificationType.WonAuction);
        dto.Title.Should().Be("You won!");
        dto.Message.Should().Be("Final price $500");
        dto.IsRead.Should().BeFalse();
        dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task HandleAsync_WithOnlyUnread_PassesFlagToRepository()
    {
        var userId = Guid.NewGuid();
        _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<Notification>());
        _repository.CountByUserIdAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(0);
        var query = CreateQuery(userId);
        query.OnlyUnread = true;

        await _handler.HandleAsync(query);

        await _repository.Received(1).GetByUserIdAsync(userId, 1, 20, true, Arg.Any<CancellationToken>());
        await _repository.Received(1).CountByUserIdAsync(userId, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithEmptyResult_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        _repository.GetByUserIdAsync(userId, 1, 20, false, Arg.Any<CancellationToken>())
            .Returns(new List<Notification>());
        _repository.CountByUserIdAsync(userId, false, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _handler.HandleAsync(CreateQuery(userId));

        result.Value.Notifications.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _repository.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB error"));

        var result = await _handler.HandleAsync(CreateQuery());

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(500);
    }
}
