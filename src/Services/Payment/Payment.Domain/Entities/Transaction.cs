using AuHub.Shared.ValueObjects;
using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; } = Money.Zero;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public Guid? ReferenceId { get; private set; }

    private Transaction() { }

    public static Transaction Create(
        Guid userId,
        TransactionType type,
        Money amount,
        string description,
        Guid? referenceId = null)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Amount = amount,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            ReferenceId = referenceId
        };
    }
}
