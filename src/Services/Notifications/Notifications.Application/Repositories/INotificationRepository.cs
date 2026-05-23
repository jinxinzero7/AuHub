using Notifications.Domain.Entities;

namespace Notifications.Application.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Notification>> GetByUserIdAsync(Guid userId, int page, int pageSize, bool onlyUnread, CancellationToken ct = default);
    Task<int> CountByUserIdAsync(Guid userId, bool onlyUnread, CancellationToken ct = default);
    Task UpdateAsync(Notification notification, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
