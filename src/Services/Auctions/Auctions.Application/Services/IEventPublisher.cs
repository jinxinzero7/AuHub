namespace Auctions.Application.Services;

public interface IEventPublisher
{
    Task PublishNewBidAsync(Guid lotId, decimal newPrice, string bidderName, CancellationToken ct = default);
    Task PublishLotCompletedAsync(Guid lotId, string title, decimal finalPrice, string? winnerName, CancellationToken ct = default);
    Task PublishUserNotificationAsync(Guid userId, string type, string message, Guid? lotId, CancellationToken ct = default);
}
