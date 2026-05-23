using AuHub.Shared.Contracts;
using MassTransit;
using Notifications.Application.Commands.SendNotification;
using Notifications.Domain.Enums;

namespace Notifications.API.Consumers;

public class AuctionCompletedConsumer(SendNotificationCommandHandler handler, ILogger<AuctionCompletedConsumer> logger) : IConsumer<AuctionCompletedEvent>
{
    public async Task Consume(ConsumeContext<AuctionCompletedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Consuming AuctionCompletedEvent for Lot {LotId}", message.LotId);

        if (message.WinnerId.HasValue)
        {
            await handler.HandleAsync(new SendNotificationCommand
            {
                UserId = message.WinnerId.Value,
                Type = NotificationType.WonAuction,
                Title = "Вы выиграли аукцион!",
                Message = $"Вы выиграли лот «{message.LotTitle}»! Финальная цена: {message.FinalPrice:C2}",
            });
        }
    }
}
