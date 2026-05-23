using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Repositories;
using Notifications.Infrastructure.Data;
using Notifications.Infrastructure.Repositories;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName)));

        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }
}
