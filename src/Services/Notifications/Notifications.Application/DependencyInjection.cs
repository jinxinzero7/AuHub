using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Commands.SendNotification;
using Notifications.Application.Commands.MarkAsRead;
using Notifications.Application.Queries.GetUserNotifications;
using Notifications.Application.Queries.GetUnreadCount;

namespace Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SendNotificationCommandHandler>();
        services.AddScoped<MarkAsReadCommandHandler>();
        services.AddScoped<GetUserNotificationsQueryHandler>();
        services.AddScoped<GetUnreadCountQueryHandler>();

        return services;
    }
}
