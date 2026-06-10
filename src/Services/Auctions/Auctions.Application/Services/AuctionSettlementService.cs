using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Auctions.Domain.Entities;

namespace Auctions.Application.Services;

public class AuctionSettlementService
{
    public const decimal ServiceCommissionRate = 0.01m;

    private readonly IPaymentClient _paymentClient;

    public AuctionSettlementService(IPaymentClient paymentClient)
    {
        _paymentClient = paymentClient;
    }

    public static Money CalculateServiceFee(Money finalPrice)
    {
        return finalPrice * ServiceCommissionRate;
    }

    public static Money CalculateSellerPayout(Money finalPrice)
    {
        return finalPrice - CalculateServiceFee(finalPrice);
    }

    public async Task<Result<bool>> ChargeWinnerAsync(Lot lot, CancellationToken cancellationToken = default)
    {
        if (!lot.WinnerId.HasValue || lot.CurrentPrice <= Money.Zero)
            return Result.Success(true);

        var result = await _paymentClient.ChargeWinnerAsync(
            lot.WinnerId.Value,
            lot.CurrentPrice.Amount,
            lot.Id,
            cancellationToken);

        return result.Success
            ? Result.Success(true)
            : Result.Failure<bool>(result.Message, 503);
    }

    public async Task<Result<Money>> PaySellerAsync(Lot lot, CancellationToken cancellationToken = default)
    {
        var sellerPayout = CalculateSellerPayout(lot.CurrentPrice);
        var serviceFee = CalculateServiceFee(lot.CurrentPrice);
        var result = await _paymentClient.TransferToSellerAsync(
            lot.SellerId,
            sellerPayout.Amount,
            serviceFee.Amount,
            lot.Id,
            cancellationToken);

        return result.Success
            ? Result.Success(sellerPayout)
            : Result.Failure<Money>(result.Message, 503);
    }

    public async Task<Result<bool>> RefundWinnerAsync(Lot lot, CancellationToken cancellationToken = default)
    {
        if (!lot.WinnerId.HasValue || lot.CurrentPrice <= Money.Zero)
            return Result.Success(true);

        var result = await _paymentClient.RefundFundsAsync(
            lot.WinnerId.Value,
            lot.CurrentPrice.Amount,
            lot.Id,
            cancellationToken);

        return result.Success
            ? Result.Success(true)
            : Result.Failure<bool>(result.Message, 503);
    }
}
