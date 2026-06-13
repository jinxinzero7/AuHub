using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using Auctions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Auctions.Infrastructure.Repositories;

public class TrustScoreEventRepository : ITrustScoreEventRepository
{
    private readonly AuctionsDbContext _context;

    public TrustScoreEventRepository(AuctionsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(
        Guid userId,
        TrustScoreSubject subject,
        TrustScoreReason reason,
        Guid referenceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TrustScoreEvents.AnyAsync(
            trustEvent =>
                trustEvent.UserId == userId &&
                trustEvent.Subject == subject &&
                trustEvent.Reason == reason &&
                trustEvent.ReferenceId == referenceId,
            cancellationToken);
    }

    public async Task<List<TrustScoreEvent>> GetByUserIdAsync(
        Guid userId,
        TrustScoreSubject subject,
        CancellationToken cancellationToken = default)
    {
        return await _context.TrustScoreEvents
            .Where(trustEvent => trustEvent.UserId == userId && trustEvent.Subject == subject)
            .OrderByDescending(trustEvent => trustEvent.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TrustScoreEvent trustScoreEvent, CancellationToken cancellationToken = default)
    {
        await _context.TrustScoreEvents.AddAsync(trustScoreEvent, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
