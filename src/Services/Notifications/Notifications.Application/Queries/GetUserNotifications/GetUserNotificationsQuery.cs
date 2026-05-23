namespace Notifications.Application.Queries.GetUserNotifications;

public class GetUserNotificationsQuery
{
    public Guid UserId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool OnlyUnread { get; set; }
}
