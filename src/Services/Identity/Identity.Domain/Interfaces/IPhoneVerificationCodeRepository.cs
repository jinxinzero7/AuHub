using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces;

public interface IPhoneVerificationCodeRepository
{
    Task<PhoneVerificationCode?> GetActiveByUserIdAndHashAsync(
        Guid userId,
        string codeHash,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task AddAsync(PhoneVerificationCode code, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
