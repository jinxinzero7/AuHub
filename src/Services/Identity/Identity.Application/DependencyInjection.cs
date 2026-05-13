using Identity.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register AuthService
        services.AddScoped<IAuthService, AuthService>();

        // Register Command Handlers
        services.AddScoped<Commands.Auth.Register.RegisterCommandHandler>();
        services.AddScoped<Commands.Auth.Login.LoginCommandHandler>();
        services.AddScoped<Commands.Auth.RefreshToken.RefreshTokenCommandHandler>();

        return services;
    }
}
