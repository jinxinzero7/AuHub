using Auctions.Application.Commands.UpdateLot;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace Auctions.UnitTests;

public class UpdateLotCommandHandlerTests
{
    private readonly ILotRepository _lotRepository = Substitute.For<ILotRepository>();
    private readonly UpdateLotCommandHandler _handler;

    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid LotId = Guid.NewGuid();

    public UpdateLotCommandHandlerTests()
    {
        _handler = new UpdateLotCommandHandler(_lotRepository);
    }

    [Fact]
    public async Task HandleAsync_OwnerDraftLot_UpdatesLot()
    {
        var lot = CreateDraftLot();
        _lotRepository.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);

        var result = await _handler.HandleAsync(CreateCommand(submitForModeration: false), SellerId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Draft");
        lot.Title.Should().Be("Updated Lot");
        lot.SupportedDeliveryProviders.Should().Equal(DeliveryProvider.Cdek, DeliveryProvider.YandexDelivery);
        await _lotRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RejectedLotWithSubmitFlag_ReturnsToModeration()
    {
        var lot = CreateDraftLot();
        lot.SubmitForModeration();
        lot.Reject("Fix it");
        _lotRepository.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);

        var result = await _handler.HandleAsync(CreateCommand(submitForModeration: true), SellerId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("PendingModeration");
        lot.Status.Should().Be(LotStatus.PendingModeration);
        lot.AdminComment.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_NotOwner_ReturnsForbidden()
    {
        var lot = CreateDraftLot();
        _lotRepository.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);

        var result = await _handler.HandleAsync(CreateCommand(submitForModeration: false), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(403);
        await _lotRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PendingModeration_ReturnsBadRequest()
    {
        var lot = CreateDraftLot();
        lot.SubmitForModeration();
        _lotRepository.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);

        var result = await _handler.HandleAsync(CreateCommand(submitForModeration: false), SellerId);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Be("Only draft or rejected lots can be edited");
    }

    private static Lot CreateDraftLot()
    {
        return Lot.Create("Lot", "Description", Money.FromDecimal(1000m), TimeSpan.FromDays(3), SellerId, [DeliveryProvider.Cdek]);
    }

    private static UpdateLotCommand CreateCommand(bool submitForModeration)
    {
        return new UpdateLotCommand
        {
            LotId = LotId,
            Title = "Updated Lot",
            Description = "Updated description",
            StartingPrice = Money.FromDecimal(2500m),
            DurationHours = 48,
            SupportedDeliveryProviders = [DeliveryProvider.Cdek, DeliveryProvider.YandexDelivery],
            SubmitForModeration = submitForModeration
        };
    }
}
