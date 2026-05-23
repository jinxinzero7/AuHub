using Auctions.Domain.Enums;

namespace Auctions.Application.Services;

public interface INotificationClient
{
    Task SendNotificationAsync(Guid userId, NotificationType type, string title, string message, CancellationToken ct = default);
}
