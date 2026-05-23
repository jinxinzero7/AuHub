using Auctions.Domain.Entities;

namespace Auctions.Domain.Interfaces;

public interface IBidRepository
{
    Task AddAsync(Bid bid, CancellationToken cancellationToken = default);
    Task<List<Bid>> GetByLotIdAsync(Guid lotId, CancellationToken cancellationToken = default);
    Task<List<Bid>> GetByBidderIdAsync(Guid bidderId, CancellationToken cancellationToken = default);
    Task<Bid?> GetByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
