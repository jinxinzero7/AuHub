using System.Reflection;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Auctions.UnitTests;

public class AuctionSettlementServiceTests
{
    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid WinnerId = Guid.NewGuid();

    private readonly IPaymentClient _paymentClient;
    private readonly AuctionSettlementService _service;

    public AuctionSettlementServiceTests()
    {
        _paymentClient = Substitute.For<IPaymentClient>();
        _service = new AuctionSettlementService(_paymentClient);
    }

    private static Lot CreateCompletedLotWithWinner(decimal finalPrice = 1000m)
    {
        var lot = Lot.Create(
            "Test Lot",
            "Description",
            Money.FromDecimal(100m),
            TimeSpan.FromDays(1),
            SellerId,
            [DeliveryProvider.Cdek]);

        lot.SubmitForModeration();
        lot.Approve();
        lot.PlaceBid(Money.FromDecimal(finalPrice), WinnerId, "Winner");

        var bidsField = typeof(Lot).GetField("_bids", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var bids = (List<Bid>)bidsField.GetValue(lot)!;
        bids.Add(Bid.Create(lot.Id, WinnerId, Money.FromDecimal(finalPrice)));

        lot.Complete();
        return lot;
    }

    [Fact]
    public void CalculateSellerPayout_UsesOnePercentServiceFee()
    {
        var payout = AuctionSettlementService.CalculateSellerPayout(Money.FromDecimal(1000m));

        payout.Should().Be(Money.FromDecimal(990m));
    }

    [Fact]
    public void CalculateServiceFee_UsesOnePercent()
    {
        var fee = AuctionSettlementService.CalculateServiceFee(Money.FromDecimal(1000m));

        fee.Should().Be(Money.FromDecimal(10m));
    }

    [Fact]
    public async Task ChargeWinnerAsync_ChargesWinnerButDoesNotPaySeller()
    {
        var lot = CreateCompletedLotWithWinner();
        _paymentClient.ChargeWinnerAsync(WinnerId, 1000m, lot.Id, Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        var result = await _service.ChargeWinnerAsync(lot);

        result.IsSuccess.Should().BeTrue();
        await _paymentClient.Received(1).ChargeWinnerAsync(WinnerId, 1000m, lot.Id, Arg.Any<CancellationToken>());
        await _paymentClient.DidNotReceive().TransferToSellerAsync(
            Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PaySellerAsync_TransfersFinalPriceMinusOnePercentFee()
    {
        var lot = CreateCompletedLotWithWinner();
        _paymentClient.TransferToSellerAsync(SellerId, 990m, 10m, lot.Id, Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        var result = await _service.PaySellerAsync(lot);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Money.FromDecimal(990m));
        await _paymentClient.Received(1).TransferToSellerAsync(SellerId, 990m, 10m, lot.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefundWinnerAsync_RefundsFullFinalPrice()
    {
        var lot = CreateCompletedLotWithWinner();
        _paymentClient.RefundFundsAsync(WinnerId, 1000m, lot.Id, Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        var result = await _service.RefundWinnerAsync(lot);

        result.IsSuccess.Should().BeTrue();
        await _paymentClient.Received(1).RefundFundsAsync(WinnerId, 1000m, lot.Id, Arg.Any<CancellationToken>());
        await _paymentClient.DidNotReceive().TransferToSellerAsync(
            Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefundWinnerAsync_WhenPaymentFails_ReturnsFailure()
    {
        var lot = CreateCompletedLotWithWinner();
        _paymentClient.RefundFundsAsync(WinnerId, 1000m, lot.Id, Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(false, "Payment unavailable"));

        var result = await _service.RefundWinnerAsync(lot);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Payment unavailable");
        result.StatusCode.Should().Be(503);
    }
}
