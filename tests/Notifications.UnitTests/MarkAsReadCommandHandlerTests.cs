using AuHub.Shared.Results;
using FluentAssertions;
using Notifications.Application.Commands.MarkAsRead;
using Notifications.Application.Repositories;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Notifications.UnitTests;

public class MarkAsReadCommandHandlerTests
{
    private readonly INotificationRepository _repository;
    private readonly MarkAsReadCommandHandler _handler;

    public MarkAsReadCommandHandlerTests()
    {
        _repository = Substitute.For<INotificationRepository>();
        _handler = new MarkAsReadCommandHandler(_repository);
    }

    private MarkAsReadCommand CreateCommand(Guid? notificationId = null, Guid? userId = null)
    {
        return new MarkAsReadCommand
        {
            NotificationId = notificationId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid()
        };
    }

    private Notification CreateNotification(Guid userId)
    {
        return Notification.Create(userId, NotificationType.NewBid, "Title", "Message");
    }

    [Fact]
    public async Task HandleAsync_WithValidData_MarksAsRead()
    {
        var userId = Guid.NewGuid();
        var notification = CreateNotification(userId);
        var command = CreateCommand(notification.Id, userId);
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        await _repository.Received(1).UpdateAsync(notification, Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenNotificationNotFound_ReturnsFailure()
    {
        var command = CreateCommand();
        _repository.GetByIdAsync(command.NotificationId, Arg.Any<CancellationToken>()).Returns((Notification?)null);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Notification not found");
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsNotOwner_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var notification = CreateNotification(ownerId);
        var command = CreateCommand(notification.Id, otherUserId);
        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("You can only mark your own notifications as read");
        result.StatusCode.Should().Be(403);
        notification.IsRead.Should().BeFalse();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB error"));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(500);
    }
}
