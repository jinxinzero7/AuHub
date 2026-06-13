using Identity.Application.Services;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure.Data;

public static class AdminBootstrapper
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var email = configuration["AdminBootstrap:Email"];

        if (string.IsNullOrWhiteSpace(email))
            return;

        var password = configuration["AdminBootstrap:Password"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("AdminBootstrap:Password must be configured when AdminBootstrap:Email is set.");

        var name = configuration["AdminBootstrap:Name"];
        if (string.IsNullOrWhiteSpace(name))
            name = "AuHub Admin";

        var phoneNumber = configuration["AdminBootstrap:PhoneNumber"];
        if (string.IsNullOrWhiteSpace(phoneNumber))
            phoneNumber = "+70000000000";

        var nickname = configuration["AdminBootstrap:Nickname"];
        if (string.IsNullOrWhiteSpace(nickname))
            nickname = "auhub_admin";

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var dbContext = services.GetRequiredService<IdentityDbContext>();
        var existingUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (existingUser != null)
        {
            if (existingUser.Role != UserRole.Admin)
                throw new InvalidOperationException("Configured bootstrap admin email already belongs to a non-admin user.");

            return;
        }

        var authService = services.GetRequiredService<IAuthService>();
        var admin = User.Create(
            normalizedEmail,
            phoneNumber,
            nickname,
            authService.HashPassword(password),
            name.Trim(),
            UserRole.Admin);

        admin.MarkEmailVerified();
        admin.MarkPhoneVerified();

        await dbContext.Users.AddAsync(admin, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
