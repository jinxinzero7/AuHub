using AuHub.Shared.ValueObjects;

namespace Auctions.Application.Commands.CreateLot;

public record CreateLotCommand
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Money StartingPrice { get; init; } = Money.Zero;
    public int DurationHours { get; init; }
}
