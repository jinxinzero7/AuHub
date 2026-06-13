using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class PhoneVerificationCodeRepository : IPhoneVerificationCodeRepository
{
    private readonly IdentityDbContext _context;

    public PhoneVerificationCodeRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<PhoneVerificationCode?> GetActiveByUserIdAndHashAsync(
        Guid userId,
        string codeHash,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return await _context.PhoneVerificationCodes
            .FirstOrDefaultAsync(
                code => code.UserId == userId &&
                    code.CodeHash == codeHash &&
                    code.UsedAt == null &&
                    code.ExpiresAt > now,
                cancellationToken);
    }

    public async Task AddAsync(PhoneVerificationCode code, CancellationToken cancellationToken = default)
    {
        await _context.PhoneVerificationCodes.AddAsync(code, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
