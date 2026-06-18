using System.Reflection;
using AuHub.Shared.ValueObjects;
using Auctions.Application.Queries.GetAdminUserActivity;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Auctions.UnitTests;

public class GetAdminUserActivityQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_AggregatesCountsRatingTrustAndPagination()
    {
        var userId = Guid.NewGuid();
        var activeLot = CreateActiveLot(userId, "Active lot");
        var draftLot = CreateDraftLot(userId, "Draft lot");
        var bid = Bid.Create(activeLot.Id, userId, Money.FromDecimal(1500m));
        typeof(Bid).GetProperty(nameof(Bid.Lot), BindingFlags.Instance | BindingFlags.Public)!.SetValue(bid, activeLot);
        var review = Review.Create(activeLot.Id, userId, Guid.NewGuid(), 4, "Good seller");
        var sellerTrust = TrustScoreEvent.Create(userId, TrustScoreSubject.Seller, TrustScoreReason.SuccessfulSale, 5, "Lot", activeLot.Id);
        var buyerTrust = TrustScoreEvent.Create(userId, TrustScoreSubject.Buyer, TrustScoreReason.DeliveryRequestExpired, -5, "Lot", Guid.NewGuid());
        var lots = Substitute.For<ILotRepository>();
        var bids = Substitute.For<IBidRepository>();
        var reviews = Substitute.For<IReviewRepository>();
        var trust = Substitute.For<ITrustScoreEventRepository>();
        lots.GetBySellerIdAsync(userId, true, Arg.Any<CancellationToken>()).Returns([activeLot, draftLot]);
        lots.GetByWinnerIdAsync(userId, Arg.Any<CancellationToken>()).Returns([activeLot]);
        bids.GetByBidderIdAsync(userId, Arg.Any<CancellationToken>()).Returns([bid]);
        reviews.GetBySellerIdAsync(userId, Arg.Any<CancellationToken>()).Returns([review]);
        trust.GetByUserIdAsync(userId, TrustScoreSubject.Seller, Arg.Any<CancellationToken>()).Returns([sellerTrust]);
        trust.GetByUserIdAsync(userId, TrustScoreSubject.Buyer, Arg.Any<CancellationToken>()).Returns([buyerTrust]);
        var handler = new GetAdminUserActivityQueryHandler(lots, bids, reviews, trust);

        var result = await handler.HandleAsync(userId, page: 2, pageSize: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedLotsCount.Should().Be(2);
        result.Value.BidsCount.Should().Be(1);
        result.Value.WinsCount.Should().Be(1);
        result.Value.LotStatusCounts.Should().ContainKey("Active").WhoseValue.Should().Be(1);
        result.Value.CreatedLots.Items.Should().ContainSingle(item => item.LotId == draftLot.Id);
        result.Value.CreatedLots.TotalPages.Should().Be(2);
        result.Value.RecentBids.Should().ContainSingle(item => item.LotId == activeLot.Id);
        result.Value.SellerRating.AverageRating.Should().Be(4);
        result.Value.SellerTrust.Score.Should().Be(75);
        result.Value.RecentTrustEvents.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_NoAuctionsData_ReturnsEmptyActivity()
    {
        var lots = Substitute.For<ILotRepository>();
        var bids = Substitute.For<IBidRepository>();
        var reviews = Substitute.For<IReviewRepository>();
        var trust = Substitute.For<ITrustScoreEventRepository>();
        lots.GetBySellerIdAsync(Arg.Any<Guid>(), true, Arg.Any<CancellationToken>()).Returns([]);
        lots.GetByWinnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        bids.GetByBidderIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        reviews.GetBySellerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        trust.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<TrustScoreSubject>(), Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetAdminUserActivityQueryHandler(lots, bids, reviews, trust);

        var result = await handler.HandleAsync(Guid.NewGuid(), 1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedLots.Items.Should().BeEmpty();
        result.Value.CreatedLots.TotalCount.Should().Be(0);
        result.Value.SellerTrust.Score.Should().Be(70);
    }

    private static Lot CreateDraftLot(Guid sellerId, string title) => Lot.Create(
        title, "Description", Money.FromDecimal(1000m), TimeSpan.FromDays(1), sellerId, [DeliveryProvider.Cdek]);

    private static Lot CreateActiveLot(Guid sellerId, string title)
    {
        var lot = CreateDraftLot(sellerId, title);
        lot.SubmitForModeration();
        lot.Approve();
        return lot;
    }
}
