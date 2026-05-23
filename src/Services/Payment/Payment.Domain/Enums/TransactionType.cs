namespace Payment.Domain.Enums;

public enum TransactionType
{
    Deposit = 0,
    Withdraw = 1,
    Reserve = 2,
    Release = 3,
    Win = 4,
    Transfer = 5,
    Refund = 6,
    DisputeRefund = 7
}
