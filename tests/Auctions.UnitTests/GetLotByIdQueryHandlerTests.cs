using Auctions.Application.Queries.GetLotById;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Auctions.UnitTests;

public class GetLotByIdQueryHandlerTests
{
    [Fact]
    public async Task AnonymousRequester_CannotReadDraftLot()
    {
        var lot = CreateLot(active: false);
        var handler = CreateHandler(lot);

        var result = await handler.HandleAsync(new GetLotByIdQuery { LotId = lot.Id });

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Seller_CanReadOwnDraftLot()
    {
        var lot = CreateLot(active: false);
        var handler = CreateHandler(lot);

        var result = await handler.HandleAsync(new GetLotByIdQuery
        {
            LotId = lot.Id,
            RequesterUserId = lot.SellerId
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(lot.Id);
    }

    [Fact]
    public async Task AnonymousRequester_CanReadActiveLot()
    {
        var lot = CreateLot(active: true);
        var handler = CreateHandler(lot);

        var result = await handler.HandleAsync(new GetLotByIdQuery { LotId = lot.Id });

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Admin_CanReadDraftLot()
    {
        var lot = CreateLot(active: false);
        var handler = CreateHandler(lot);

        var result = await handler.HandleAsync(new GetLotByIdQuery
        {
            LotId = lot.Id,
            RequesterIsAdmin = true
        });

        result.IsSuccess.Should().BeTrue();
    }

    private static GetLotByIdQueryHandler CreateHandler(Lot lot)
    {
        var repository = Substitute.For<ILotRepository>();
        repository.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        return new GetLotByIdQueryHandler(repository);
    }

    private static Lot CreateLot(bool active)
    {
        var lot = Lot.Create(
            "Visibility lot",
            "Description",
            Money.FromDecimal(1000m),
            TimeSpan.FromHours(24),
            Guid.NewGuid(),
            [DeliveryProvider.Cdek]);

        if (active)
        {
            lot.SubmitForModeration();
            lot.Approve();
        }

        return lot;
    }
}
