using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces;

public interface IDocumentVerificationRequestRepository
{
    Task<DocumentVerificationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentVerificationRequest?> GetPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<DocumentVerificationRequest>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<List<DocumentVerificationRequest>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(DocumentVerificationRequest request, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
