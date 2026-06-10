namespace Payment.Domain.Enums;

public enum WalletTransactionEffect
{
    AvailableCredit = 1,
    AvailableDebit = 2,
    Freeze = 3,
    Release = 4,
    FrozenDebit = 5
}
