using Auctions.Domain.Entities;

namespace Auctions.Domain.Interfaces;

public interface ILotImageRepository
{
    Task<LotImage?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LotImage?> GetByFileNameAsync(Guid lotId, string fileName, CancellationToken ct = default);
    Task<LotImage?> GetByObjectNameAsync(Guid lotId, string objectName, CancellationToken ct = default);
    Task<IReadOnlyList<LotImage>> GetByLotIdAsync(Guid lotId, CancellationToken ct = default);
    Task AddAsync(LotImage image, CancellationToken ct = default);
    void Remove(LotImage image);
    Task SaveChangesAsync(CancellationToken ct = default);
}
