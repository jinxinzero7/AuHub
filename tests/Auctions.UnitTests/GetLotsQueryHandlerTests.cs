using AuHub.Shared.ValueObjects;
using Auctions.Application.Queries.GetLots;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Auctions.UnitTests;

public class GetLotsQueryHandlerTests
{
    private readonly ILotRepository _lotRepository = Substitute.For<ILotRepository>();

    [Fact]
    public async Task HandleAsync_ActiveSellerQuery_FiltersSellerStatusAndDeletedLots()
    {
        var sellerId = Guid.NewGuid();
        var activeLot = CreateActiveLot(sellerId, "Public active lot");
        var draftLot = CreateDraftLot(sellerId, "Private draft lot");
        var otherSellerLot = CreateActiveLot(Guid.NewGuid(), "Other seller lot");
        var deletedLot = CreateActiveLot(sellerId, "Deleted lot");
        deletedLot.SoftDelete(sellerId);

        _lotRepository.GetBySellerIdAsync(sellerId, false, Arg.Any<CancellationToken>())
            .Returns([activeLot, draftLot, otherSellerLot, deletedLot]);
        var handler = new GetLotsQueryHandler(_lotRepository);

        var result = await handler.HandleAsync(new GetLotsQuery
        {
            SellerId = sellerId.ToString(),
            OnlyActive = true,
            Page = 1,
            PageSize = 9
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Lots.Should().ContainSingle(lot => lot.Id == activeLot.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_ActiveSellerQuery_AppliesPagination()
    {
        var sellerId = Guid.NewGuid();
        var lots = Enumerable.Range(1, 12)
            .Select(index => CreateActiveLot(sellerId, $"Lot {index}"))
            .ToList();

        _lotRepository.GetBySellerIdAsync(sellerId, false, Arg.Any<CancellationToken>())
            .Returns(lots);
        var handler = new GetLotsQueryHandler(_lotRepository);

        var result = await handler.HandleAsync(new GetLotsQuery
        {
            SellerId = sellerId.ToString(),
            OnlyActive = true,
            Page = 2,
            PageSize = 5
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Lots.Select(lot => lot.Id).Should().Equal(lots.Skip(5).Take(5).Select(lot => lot.Id));
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalCount.Should().Be(12);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_ActiveSellerQuery_WithNoLots_ReturnsEmptyPage()
    {
        var sellerId = Guid.NewGuid();
        _lotRepository.GetBySellerIdAsync(sellerId, false, Arg.Any<CancellationToken>())
            .Returns([]);
        var handler = new GetLotsQueryHandler(_lotRepository);

        var result = await handler.HandleAsync(new GetLotsQuery
        {
            SellerId = sellerId.ToString(),
            OnlyActive = true,
            Page = 1,
            PageSize = 9
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Lots.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    private static Lot CreateDraftLot(Guid sellerId, string title)
    {
        return Lot.Create(
            title,
            "Description",
            Money.FromDecimal(1000m),
            TimeSpan.FromDays(1),
            sellerId,
            [DeliveryProvider.Cdek]);
    }

    private static Lot CreateActiveLot(Guid sellerId, string title)
    {
        var lot = CreateDraftLot(sellerId, title);
        lot.SubmitForModeration();
        lot.Approve();
        return lot;
    }
}
