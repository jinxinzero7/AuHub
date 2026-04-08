using Auctions.Domain.Entities;

namespace Auctions.Domain.Interfaces;

public interface ILotRepository
{
    Task<Lot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Lot>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Lot>> GetActiveLotsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Lot lot, CancellationToken cancellationToken = default);
    Task UpdateAsync(Lot lot, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
