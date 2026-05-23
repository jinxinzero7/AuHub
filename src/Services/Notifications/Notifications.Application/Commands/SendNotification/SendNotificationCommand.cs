using Notifications.Domain.Enums;

namespace Notifications.Application.Commands.SendNotification;

public class SendNotificationCommand
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
