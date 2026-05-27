using AuHub.Shared.Results;
using Notifications.Application.Repositories;
using Notifications.Domain.Entities;

namespace Notifications.Application.Commands.SendNotification;

public class SendNotificationCommandHandler
{
    private readonly INotificationRepository _repository;

    public SendNotificationCommandHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> HandleAsync(SendNotificationCommand command, CancellationToken ct = default)
    {
        try
        {
            var notification = Notification.Create(command.UserId, command.Type, command.Title, command.Message);

            await _repository.AddAsync(notification, ct);
            await _repository.SaveChangesAsync(ct);

            return Result.Success(notification.Id);
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>($"Failed to send notification: {ex.Message}", 500);
        }
    }
}
