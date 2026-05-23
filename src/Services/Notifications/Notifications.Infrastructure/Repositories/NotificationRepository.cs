using Microsoft.EntityFrameworkCore;
using Notifications.Application.Repositories;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.Data;

namespace Notifications.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _context;

    public NotificationRepository(NotificationsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        await _context.Notifications.AddAsync(notification, ct);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Notifications.FindAsync([id], cancellationToken: ct);
    }

    public async Task<List<Notification>> GetByUserIdAsync(Guid userId, int page, int pageSize, bool onlyUnread, CancellationToken ct = default)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (onlyUnread)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountByUserIdAsync(Guid userId, bool onlyUnread, CancellationToken ct = default)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (onlyUnread)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.CountAsync(ct);
    }

    public Task UpdateAsync(Notification notification, CancellationToken ct = default)
    {
        _context.Notifications.Update(notification);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
