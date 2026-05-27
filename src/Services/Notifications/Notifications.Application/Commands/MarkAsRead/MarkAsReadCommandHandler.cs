using AuHub.Shared.Results;
using Notifications.Application.Repositories;

namespace Notifications.Application.Commands.MarkAsRead;

public class MarkAsReadCommandHandler
{
    private readonly INotificationRepository _repository;

    public MarkAsReadCommandHandler(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<bool>> HandleAsync(MarkAsReadCommand command, CancellationToken ct = default)
    {
        try
        {
            var notification = await _repository.GetByIdAsync(command.NotificationId, ct);

            if (notification == null)
            {
                return Result.Failure<bool>("Notification not found", 404);
            }

            if (notification.UserId != command.UserId)
            {
                return Result.Failure<bool>("You can only mark your own notifications as read", 403);
            }

            notification.MarkAsRead();
            await _repository.UpdateAsync(notification, ct);
            await _repository.SaveChangesAsync(ct);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>($"Failed to mark notification as read: {ex.Message}", 500);
        }
    }
}
