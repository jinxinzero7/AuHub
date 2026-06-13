using Identity.Application.Services;
using Identity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

public class DevPhoneVerificationSender : IPhoneVerificationSender
{
    private readonly ILogger<DevPhoneVerificationSender> _logger;

    public DevPhoneVerificationSender(ILogger<DevPhoneVerificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(User user, string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Phone verification code for user {UserId} and phone {PhoneNumber}: {Code}",
            user.Id,
            user.PhoneNumber,
            code);

        return Task.CompletedTask;
    }
}
