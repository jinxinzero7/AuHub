using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class DocumentVerificationRequestRepository : IDocumentVerificationRequestRepository
{
    private readonly IdentityDbContext _context;

    public DocumentVerificationRequestRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentVerificationRequest?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.DocumentVerificationRequests
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);
    }

    public async Task<DocumentVerificationRequest?> GetPendingByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DocumentVerificationRequests
            .Where(request => request.UserId == userId && request.Status == DocumentVerificationRequestStatus.PendingReview)
            .OrderByDescending(request => request.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<DocumentVerificationRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DocumentVerificationRequests
            .Where(request => request.Status == DocumentVerificationRequestStatus.PendingReview)
            .OrderBy(request => request.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DocumentVerificationRequest>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DocumentVerificationRequests
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DocumentVerificationRequest request, CancellationToken cancellationToken = default)
    {
        await _context.DocumentVerificationRequests.AddAsync(request, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
