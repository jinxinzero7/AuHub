using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;
using Auctions.Infrastructure.Data;

namespace Auctions.Infrastructure.Repositories;

public class AdminAuditLogRepository : IAdminAuditLogRepository
{
    private readonly AuctionsDbContext _context;

    public AdminAuditLogRepository(AuctionsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AdminAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.AdminAuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
