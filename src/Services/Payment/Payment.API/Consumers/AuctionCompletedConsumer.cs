using AuHub.Shared.Contracts;
using AuHub.Shared.ValueObjects;
using MassTransit;
using Payment.Application.Commands.ChargeWinner;
using Payment.Application.Commands.TransferToSeller;

namespace Payment.API.Consumers;

public class AuctionCompletedConsumer(
    ChargeWinnerCommandHandler chargeHandler,
    TransferToSellerCommandHandler transferHandler,
    ILogger<AuctionCompletedConsumer> logger) : IConsumer<AuctionCompletedEvent>
{
    public async Task Consume(ConsumeContext<AuctionCompletedEvent> context)
    {
        var message = context.Message;
        logger.LogInformation("Consuming AuctionCompletedEvent for Lot {LotId}", message.LotId);

        if (!message.WinnerId.HasValue)
        {
            logger.LogWarning("No winner for lot {LotId}, skipping payment", message.LotId);
            return;
        }

        var finalPrice = Money.FromDecimal(message.FinalPrice);

        var chargeResult = await chargeHandler.HandleAsync(new ChargeWinnerCommand
        {
            UserId = message.WinnerId.Value,
            Amount = finalPrice,
            ReferenceId = message.LotId
        });

        if (!chargeResult.IsSuccess)
        {
            logger.LogError("Failed to charge winner for lot {LotId}: {Error}", message.LotId, chargeResult.Error);
            return;
        }

        var commission = finalPrice * 0.1m;
        var sellerAmount = finalPrice - commission;

        var transferResult = await transferHandler.HandleAsync(new TransferToSellerCommand
        {
            SellerId = message.SellerId,
            Amount = sellerAmount,
            ReferenceId = message.LotId
        });

        if (!transferResult.IsSuccess)
        {
            logger.LogError("Failed to transfer to seller for lot {LotId}: {Error}", message.LotId, transferResult.Error);
        }
    }
}
