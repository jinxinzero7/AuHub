using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly IdentityDbContext _context;

    public EmailVerificationTokenRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<EmailVerificationToken?> GetActiveByHashAsync(
        string tokenHash,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(
                token => token.TokenHash == tokenHash &&
                    token.UsedAt == null &&
                    token.ExpiresAt > now,
                cancellationToken);
    }

    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        await _context.EmailVerificationTokens.AddAsync(token, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
