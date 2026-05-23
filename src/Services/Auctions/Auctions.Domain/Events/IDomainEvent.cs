namespace Auctions.Domain.Events;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
}
