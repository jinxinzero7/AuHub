namespace Notifications.Application.Commands.MarkAsRead;

public class MarkAsReadCommand
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
}
