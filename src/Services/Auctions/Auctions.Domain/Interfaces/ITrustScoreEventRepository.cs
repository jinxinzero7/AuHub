using Auctions.Domain.Entities;
using Auctions.Domain.Enums;

namespace Auctions.Domain.Interfaces;

public interface ITrustScoreEventRepository
{
    Task<bool> ExistsAsync(Guid userId, TrustScoreSubject subject, TrustScoreReason reason, Guid referenceId, CancellationToken cancellationToken = default);
    Task<List<TrustScoreEvent>> GetByUserIdAsync(Guid userId, TrustScoreSubject subject, CancellationToken cancellationToken = default);
    Task AddAsync(TrustScoreEvent trustScoreEvent, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
