using Identity.Domain.Entities;

namespace Identity.Application.Services;

public interface IEmailVerificationSender
{
    Task SendAsync(User user, string token, CancellationToken cancellationToken = default);
}
