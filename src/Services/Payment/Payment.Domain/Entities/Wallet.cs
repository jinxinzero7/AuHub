using AuHub.Shared.ValueObjects;

namespace Payment.Domain.Entities;

public class Wallet
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Money Balance { get; private set; } = Money.Zero;
    public Money FrozenBalance { get; private set; } = Money.Zero;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Wallet() { }

    public static Wallet Create(Guid userId)
    {
        return new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = Money.Zero,
            FrozenBalance = Money.Zero,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deposit(Money amount)
    {
        if (amount <= Money.Zero)
            throw new InvalidOperationException("Deposit amount must be positive");

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Withdraw(Money amount)
    {
        if (amount <= Money.Zero)
            throw new InvalidOperationException("Withdraw amount must be positive");

        var availableBalance = Balance - FrozenBalance;
        if (availableBalance < amount)
            throw new InvalidOperationException($"Insufficient funds. Available: {availableBalance}, Required: {amount}");

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Freeze(Money amount)
    {
        if (amount <= Money.Zero)
            throw new InvalidOperationException("Freeze amount must be positive");

        if (Balance < amount)
            throw new InvalidOperationException("Insufficient funds to freeze");

        Balance -= amount;
        FrozenBalance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unfreeze(Money amount)
    {
        if (amount <= Money.Zero)
            throw new InvalidOperationException("Unfreeze amount must be positive");

        if (FrozenBalance < amount)
            throw new InvalidOperationException("Insufficient frozen funds");

        FrozenBalance -= amount;
        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void TransferFromFrozen(Money amount)
    {
        if (amount <= Money.Zero)
            throw new InvalidOperationException("Transfer amount must be positive");

        if (FrozenBalance < amount)
            throw new InvalidOperationException("Insufficient frozen funds");

        FrozenBalance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }
}
