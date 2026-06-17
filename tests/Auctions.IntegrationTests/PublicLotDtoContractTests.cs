using Auctions.API.Endpoints.Lots;
using FluentAssertions;

namespace Auctions.IntegrationTests;

public class PublicLotDtoContractTests
{
    [Theory]
    [InlineData("TrackingNumber")]
    [InlineData("DeliveryAddress")]
    [InlineData("DeliveryRecipientName")]
    [InlineData("DeliveryRecipientPhone")]
    [InlineData("SelectedDeliveryProvider")]
    [InlineData("DeliveryRequestedAt")]
    [InlineData("DeliveryRequestDeadlineAt")]
    public void PublicLotDto_DoesNotExposePrivateDeliveryRequestFields(string propertyName)
    {
        typeof(LotDto).GetProperty(propertyName).Should().BeNull();
    }
}
