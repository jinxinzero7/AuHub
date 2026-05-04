namespace Auctions.Application.Commands.PublishLot;

public record PublishLotCommand
{
    public Guid LotId { get; init; }
}
