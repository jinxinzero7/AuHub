namespace Auctions.Application.Commands.CompleteLot;

public record CompleteLotCommand
{
    public Guid LotId { get; init; }
    public Guid? ActorUserId { get; init; }
    public bool RequireSellerOwnership { get; init; }
    public bool RequireBid { get; init; }
}
