using AuHub.Shared.Results;
using FluentAssertions;
using Notifications.Application.Commands.SendNotification;
using Notifications.Application.Repositories;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Notifications.UnitTests;

public class SendNotificationCommandHandlerTests
{
    private readonly INotificationRepository _repository;
    private readonly SendNotificationCommandHandler _handler;

    public SendNotificationCommandHandlerTests()
    {
        _repository = Substitute.For<INotificationRepository>();
        _handler = new SendNotificationCommandHandler(_repository);
    }

    private SendNotificationCommand CreateCommand()
    {
        return new SendNotificationCommand
        {
            UserId = Guid.NewGuid(),
            Type = NotificationType.NewBid,
            Title = "New Bid",
            Message = "You received a new bid of $100"
        };
    }

    [Fact]
    public async Task HandleAsync_WithValidData_CreatesNotificationAndReturnsId()
    {
        var command = CreateCommand();

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Is<Notification>(n =>
            n.UserId == command.UserId &&
            n.Type == command.Type &&
            n.Title == command.Title &&
            n.Message == command.Message &&
            n.IsRead == false
        ), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsCreatedNotificationId()
    {
        var command = CreateCommand();
        Notification? captured = null;
        await _repository.AddAsync(Arg.Do<Notification>(n => captured = n), Arg.Any<CancellationToken>());

        var result = await _handler.HandleAsync(command);

        captured.Should().NotBeNull();
        result.Value.Should().Be(captured!.Id);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _repository.When(x => x.AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new Exception("DB error"));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(500);
    }
}
