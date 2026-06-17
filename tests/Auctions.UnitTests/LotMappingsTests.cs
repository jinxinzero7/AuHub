using Auctions.Application.Mappings;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using AuHub.Shared.ValueObjects;
using FluentAssertions;

namespace Auctions.UnitTests;

public class LotMappingsTests
{
    [Fact]
    public void ToDetailResponse_SellerCanSeeDeliveryRequestDetails()
    {
        var sellerId = Guid.NewGuid();
        var lot = CreateLotWithDeliveryRequest(sellerId, Guid.NewGuid());

        var response = lot.ToDetailResponse(sellerId);

        response.SelectedDeliveryProvider.Should().Be("Cdek");
        response.DeliveryAddress.Should().Be("Moscow, CDEK pickup point 42");
        response.DeliveryRecipientName.Should().Be("Winner");
        response.DeliveryRecipientPhone.Should().Be("+79990000000");
        response.DeliveryRequestedAt.Should().NotBeNull();
    }

    [Fact]
    public void ToDetailResponse_WinnerCanSeeOwnDeliveryRequestDetails()
    {
        var winnerId = Guid.NewGuid();
        var lot = CreateLotWithDeliveryRequest(Guid.NewGuid(), winnerId);

        var response = lot.ToDetailResponse(winnerId);

        response.SelectedDeliveryProvider.Should().Be("Cdek");
        response.DeliveryAddress.Should().Be("Moscow, CDEK pickup point 42");
        response.DeliveryRecipientName.Should().Be("Winner");
        response.DeliveryRecipientPhone.Should().Be("+79990000000");
        response.DeliveryRequestedAt.Should().NotBeNull();
    }

    [Fact]
    public void ToDetailResponse_AdminCanSeeDeliveryRequestDetails()
    {
        var lot = CreateLotWithDeliveryRequest(Guid.NewGuid(), Guid.NewGuid());

        var response = lot.ToDetailResponse(Guid.NewGuid(), requesterIsAdmin: true);

        response.SelectedDeliveryProvider.Should().Be("Cdek");
        response.DeliveryAddress.Should().Be("Moscow, CDEK pickup point 42");
        response.DeliveryRecipientName.Should().Be("Winner");
        response.DeliveryRecipientPhone.Should().Be("+79990000000");
        response.DeliveryRequestedAt.Should().NotBeNull();
    }

    [Fact]
    public void ToDetailResponse_UnrelatedAuthenticatedUserCannotSeeDeliveryRequestDetails()
    {
        var lot = CreateLotWithDeliveryRequest(Guid.NewGuid(), Guid.NewGuid());

        var response = lot.ToDetailResponse(Guid.NewGuid());

        response.SelectedDeliveryProvider.Should().BeNull();
        response.DeliveryAddress.Should().BeNull();
        response.DeliveryRecipientName.Should().BeNull();
        response.DeliveryRecipientPhone.Should().BeNull();
        response.DeliveryRequestedAt.Should().BeNull();
    }

    [Fact]
    public void ToDetailResponse_AnonymousUserCannotSeeDeliveryRequestDetails()
    {
        var lot = CreateLotWithDeliveryRequest(Guid.NewGuid(), Guid.NewGuid());

        var response = lot.ToDetailResponse();

        response.SelectedDeliveryProvider.Should().BeNull();
        response.DeliveryAddress.Should().BeNull();
        response.DeliveryRecipientName.Should().BeNull();
        response.DeliveryRecipientPhone.Should().BeNull();
        response.DeliveryRequestedAt.Should().BeNull();
    }

    [Fact]
    public void ToResponse_PublicListMappingDoesNotIncludeDeliveryRequestDetailsByDefault()
    {
        var lot = CreateLotWithDeliveryRequest(Guid.NewGuid(), Guid.NewGuid());

        var response = lot.ToResponse();

        response.SelectedDeliveryProvider.Should().BeNull();
        response.DeliveryAddress.Should().BeNull();
        response.DeliveryRecipientName.Should().BeNull();
        response.DeliveryRecipientPhone.Should().BeNull();
        response.DeliveryRequestedAt.Should().BeNull();
    }

    private static Lot CreateLotWithDeliveryRequest(Guid sellerId, Guid winnerId)
    {
        var lot = Lot.Create(
            "Lot",
            "Description",
            Money.FromDecimal(1000m),
            TimeSpan.FromDays(1),
            sellerId,
            [DeliveryProvider.Cdek]);

        SetProperty(lot, nameof(Lot.WinnerId), winnerId);
        SetProperty(lot, nameof(Lot.Status), LotStatus.DeliveryRequestPending);
        lot.RequestDelivery(DeliveryProvider.Cdek, "Moscow, CDEK pickup point 42", "Winner", "+79990000000");

        return lot;
    }

    private static void SetProperty<T>(object entity, string propertyName, T value)
    {
        entity.GetType().GetProperty(propertyName)!.SetValue(entity, value);
    }
}
