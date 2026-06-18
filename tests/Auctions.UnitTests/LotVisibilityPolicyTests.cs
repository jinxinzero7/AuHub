using AuHub.Shared.ValueObjects;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using FluentAssertions;

namespace Auctions.UnitTests;

public class LotVisibilityPolicyTests
{
    [Fact]
    public void CanViewDetails_Draft_AllowsOnlyOwnerOrAdmin()
    {
        var sellerId = Guid.NewGuid();
        var lot = CreateLot(sellerId);

        LotVisibilityPolicy.CanViewDetails(lot, null, false).Should().BeFalse();
        LotVisibilityPolicy.CanViewDetails(lot, Guid.NewGuid(), false).Should().BeFalse();
        LotVisibilityPolicy.CanViewDetails(lot, sellerId, false).Should().BeTrue();
        LotVisibilityPolicy.CanViewDetails(lot, null, true).Should().BeTrue();
    }

    [Fact]
    public void CanViewDetails_Active_AllowsAnonymousUnlessDeleted()
    {
        var lot = CreateLot(Guid.NewGuid());
        lot.SubmitForModeration();
        lot.Approve();

        LotVisibilityPolicy.CanViewDetails(lot, null, false).Should().BeTrue();

        lot.SoftDelete();
        LotVisibilityPolicy.CanViewDetails(lot, null, false).Should().BeFalse();
        LotVisibilityPolicy.CanViewDetails(lot, null, true).Should().BeTrue();
    }

    private static Lot CreateLot(Guid sellerId) => Lot.Create(
        "Visibility lot",
        "Description",
        Money.FromDecimal(1000m),
        TimeSpan.FromDays(1),
        sellerId,
        [DeliveryProvider.Cdek]);
}
