using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Data;

namespace Identity.Infrastructure.Repositories;

public class AdminAuditLogRepository : IAdminAuditLogRepository
{
    private readonly IdentityDbContext _context;

    public AdminAuditLogRepository(IdentityDbContext context)
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
