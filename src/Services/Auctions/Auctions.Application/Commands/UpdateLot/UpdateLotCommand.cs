using Auctions.Domain.Enums;
using AuHub.Shared.ValueObjects;

namespace Auctions.Application.Commands.UpdateLot;

public record UpdateLotCommand
{
    public Guid LotId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Money StartingPrice { get; init; } = Money.Zero;
    public int DurationHours { get; init; }
    public List<DeliveryProvider> SupportedDeliveryProviders { get; init; } = new();
    public bool SubmitForModeration { get; init; }
}
