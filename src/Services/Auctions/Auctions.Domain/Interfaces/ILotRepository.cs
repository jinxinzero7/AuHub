using Auctions.Domain.Entities;

namespace Auctions.Domain.Interfaces;

public interface ILotRepository
{
    Task<Lot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Lot>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Lot>> GetPublicLotsAsync(string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<List<Lot>> GetActiveLotsAsync(string? searchTerm = null, CancellationToken cancellationToken = default);
    Task<List<Lot>> GetBySellerIdAsync(Guid sellerId, bool includeDrafts, CancellationToken cancellationToken = default);
    Task<List<Lot>> GetByWinnerIdAsync(Guid winnerId, CancellationToken cancellationToken = default);
    Task AddAsync(Lot lot, CancellationToken cancellationToken = default);
    Task UpdateAsync(Lot lot, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
