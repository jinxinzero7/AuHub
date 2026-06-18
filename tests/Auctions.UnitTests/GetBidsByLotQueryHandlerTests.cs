using AuHub.Shared.ValueObjects;
using Auctions.Application.Queries.GetBidsByLot;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Auctions.UnitTests;

public class GetBidsByLotQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_AnonymousDraftRequest_ReturnsNotFoundWithoutLoadingBids()
    {
        var lot = CreateLot(active: false);
        var lots = Substitute.For<ILotRepository>();
        var bids = Substitute.For<IBidRepository>();
        lots.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        var handler = new GetBidsByLotQueryHandler(bids, lots);

        var result = await handler.HandleAsync(new GetBidsByLotQuery { LotId = lot.Id });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        await bids.DidNotReceive().GetByLotIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_ActiveLot_ExposesBidderIdentityOnlyToAdmin(bool isAdmin)
    {
        var lot = CreateLot(active: true);
        var bid = Bid.Create(lot.Id, Guid.NewGuid(), Money.FromDecimal(1500m));
        var lots = Substitute.For<ILotRepository>();
        var bids = Substitute.For<IBidRepository>();
        lots.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        bids.GetByLotIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns([bid]);
        var handler = new GetBidsByLotQueryHandler(bids, lots);

        var result = await handler.HandleAsync(new GetBidsByLotQuery
        {
            LotId = lot.Id,
            RequesterIsAdmin = isAdmin
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].BidderId.Should().Be(isAdmin ? bid.BidderId : null);
    }

    private static Lot CreateLot(bool active)
    {
        var lot = Lot.Create(
            "Bid lot", "Description", Money.FromDecimal(1000m), TimeSpan.FromDays(1), Guid.NewGuid(), [DeliveryProvider.Cdek]);
        if (active)
        {
            lot.SubmitForModeration();
            lot.Approve();
        }

        return lot;
    }
}
