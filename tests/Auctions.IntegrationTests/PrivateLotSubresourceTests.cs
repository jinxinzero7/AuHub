using Auctions.API.Endpoints.Images;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using AuHub.Shared.ValueObjects;
using FastEndpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Auctions.IntegrationTests;

public class PrivateLotSubresourceTests
{
    [Fact]
    public async Task GetLotImages_AnonymousDraftRequest_ReturnsNotFoundWithoutReadingImages()
    {
        var lot = CreateDraftLot();
        var storage = Substitute.For<IImageStorageService>();
        var images = Substitute.For<ILotImageRepository>();
        var lots = Substitute.For<ILotRepository>();
        lots.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        var endpoint = Factory.Create<GetLotImagesEndpoint>(ctx =>
        {
            ctx.Request.RouteValues["id"] = lot.Id;
        }, storage, images, lots);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        await images.DidNotReceive().GetByLotIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await storage.DidNotReceive().GetPresignedUrlAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetImage_AnonymousDraftRequest_ReturnsNotFoundWithoutCreatingPresignedUrl()
    {
        var lot = CreateDraftLot();
        var storage = Substitute.For<IImageStorageService>();
        var images = Substitute.For<ILotImageRepository>();
        var lots = Substitute.For<ILotRepository>();
        lots.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        var endpoint = Factory.Create<GetImageEndpoint>(ctx =>
        {
            ctx.Request.RouteValues["id"] = lot.Id;
            ctx.Request.RouteValues["fileName"] = "private.jpg";
        }, storage, images, lots);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        await images.DidNotReceive().GetByFileNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await storage.DidNotReceive().GetPresignedUrlAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private static Lot CreateDraftLot() => Lot.Create(
        "Private lot", "Description", Money.FromDecimal(1000m), TimeSpan.FromDays(1), Guid.NewGuid(), [DeliveryProvider.Cdek]);
}
