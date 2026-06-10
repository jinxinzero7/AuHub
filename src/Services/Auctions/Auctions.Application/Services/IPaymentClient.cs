namespace Auctions.Application.Services;

public interface IPaymentClient
{
    Task<BalanceResult> GetBalanceAsync(Guid userId, CancellationToken ct = default);
    Task<PaymentResult> ReserveFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default);
    Task<PaymentResult> ReleaseFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default);
    Task<PaymentResult> ChargeWinnerAsync(Guid winnerId, decimal amount, Guid lotId, CancellationToken ct = default);
    Task<PaymentResult> TransferToSellerAsync(Guid sellerId, decimal amount, decimal serviceFee, Guid lotId, CancellationToken ct = default);
    Task<PaymentResult> RefundFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default);
}

public record BalanceResult(bool Success, decimal Balance, string Message = "");
public record PaymentResult(bool Success, string Message = "");
