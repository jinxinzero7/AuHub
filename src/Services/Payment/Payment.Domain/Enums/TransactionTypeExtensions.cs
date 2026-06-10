namespace Payment.Domain.Enums;

public static class TransactionTypeExtensions
{
    public static WalletTransactionEffect GetWalletEffect(this TransactionType type)
    {
        return type switch
        {
            TransactionType.Deposit => WalletTransactionEffect.AvailableCredit,
            TransactionType.Withdraw => WalletTransactionEffect.AvailableDebit,
            TransactionType.Reserve => WalletTransactionEffect.Freeze,
            TransactionType.Release => WalletTransactionEffect.Release,
            TransactionType.Win => WalletTransactionEffect.FrozenDebit,
            TransactionType.Transfer => WalletTransactionEffect.AvailableCredit,
            TransactionType.Refund => WalletTransactionEffect.AvailableCredit,
            TransactionType.DisputeRefund => WalletTransactionEffect.AvailableCredit,
            TransactionType.ServiceFee => WalletTransactionEffect.AvailableCredit,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
