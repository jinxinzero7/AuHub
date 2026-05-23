using AuHub.Shared.Contracts;
using MassTransit;
using Notifications.Application.Commands.SendNotification;
using Notifications.Domain.Enums;

namespace Notifications.API.Consumers;

public class BidPlacedConsumer(SendNotificationCommandHandler handler, ILogger<BidPlacedConsumer> logger) : IConsumer<BidPlacedEvent>
{
    public async Task Consume(ConsumeContext<BidPlacedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Consuming BidPlacedEvent for Lot {LotId} by {BidderId}", message.LotId, message.BidderId);

        await handler.HandleAsync(new SendNotificationCommand
        {
            UserId = message.SellerId,
            Type = NotificationType.NewBid,
            Title = "Новая ставка",
            Message = $"Новая ставка на лот «{message.LotTitle}»: {message.Amount:C2} от {message.BidderName}"
        });
    }
}
