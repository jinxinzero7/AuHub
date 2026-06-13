using AuHub.Shared.ValueObjects;

namespace Payment.Application.Commands.ConfirmTopUp;

public record ConfirmTopUpCommand
{
    public Guid UserId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Guid OperationId { get; init; }
    public string Provider { get; init; } = string.Empty;
}
