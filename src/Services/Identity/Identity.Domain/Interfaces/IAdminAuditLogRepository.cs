using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces;

public interface IAdminAuditLogRepository
{
    Task AddAsync(AdminAuditLog auditLog, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
