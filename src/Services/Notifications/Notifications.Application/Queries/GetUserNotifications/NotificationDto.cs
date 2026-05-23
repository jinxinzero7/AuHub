namespace Notifications.Application.Queries.GetUserNotifications;

public class NotificationDto
{
    public Guid Id { get; init; }
    public int Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}
