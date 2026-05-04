namespace Auctions.Application.Commands.CancelLot;

public record CancelLotCommand
{
    public Guid LotId { get; init; }
}
