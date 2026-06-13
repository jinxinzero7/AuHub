using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetActiveByHashAsync(
        string tokenHash,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
