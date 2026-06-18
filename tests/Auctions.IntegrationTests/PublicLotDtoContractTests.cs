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
    [InlineData("AdminComment")]
    [InlineData("WinnerId")]
    [InlineData("BidderId")]
    [InlineData("Bids")]
    public void PublicLotDto_DoesNotExposePrivateDealFields(string propertyName)
    {
        typeof(LotDto).GetProperty(propertyName).Should().BeNull();
    }
}
