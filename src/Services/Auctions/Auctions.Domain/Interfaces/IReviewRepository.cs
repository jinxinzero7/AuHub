using Auctions.Domain.Entities;

namespace Auctions.Domain.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByLotIdAsync(Guid lotId, CancellationToken cancellationToken = default);
    Task<List<Review>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default);
    Task AddAsync(Review review, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
