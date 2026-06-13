using Identity.Application.Services;
using Identity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

public class DevEmailVerificationSender : IEmailVerificationSender
{
    private readonly ILogger<DevEmailVerificationSender> _logger;

    public DevEmailVerificationSender(ILogger<DevEmailVerificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(User user, string token, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email verification token for user {UserId} and email {Email}: {Token}",
            user.Id,
            user.Email,
            token);

        return Task.CompletedTask;
    }
}
