using Identity.Domain.Entities;

namespace Identity.Application.Services;

public interface IPhoneVerificationSender
{
    Task SendAsync(User user, string code, CancellationToken cancellationToken = default);
}
