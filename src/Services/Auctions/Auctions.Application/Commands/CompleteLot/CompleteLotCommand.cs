namespace Auctions.Application.Commands.CompleteLot;

public record CompleteLotCommand
{
    public Guid LotId { get; init; }
}
