using Identity.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<Commands.Auth.Register.RegisterCommandHandler>();
        services.AddScoped<Commands.Auth.Login.LoginCommandHandler>();
        services.AddScoped<Commands.Auth.RefreshToken.RefreshTokenCommandHandler>();
        services.AddScoped<Commands.Auth.EmailVerification.RequestEmailVerificationCommandHandler>();
        services.AddScoped<Commands.Auth.EmailVerification.ConfirmEmailVerificationCommandHandler>();
        services.AddScoped<Commands.Auth.PhoneVerification.RequestPhoneVerificationCommandHandler>();
        services.AddScoped<Commands.Auth.PhoneVerification.ConfirmPhoneVerificationCommandHandler>();

        return services;
    }
}
