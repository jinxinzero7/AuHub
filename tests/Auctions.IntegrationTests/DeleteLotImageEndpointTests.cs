using System.Security.Claims;
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

public class DeleteLotImageEndpointTests
{
    [Fact]
    public async Task HandleAsync_ImageFromAnotherLot_ReturnsNotFoundAndDoesNotDelete()
    {
        var sellerId = Guid.NewGuid();
        var lotId = Guid.NewGuid();
        var otherLotId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        var lot = Lot.Create("Lot", "Description", Money.FromDecimal(100m), TimeSpan.FromDays(1), sellerId, [DeliveryProvider.Cdek]);
        SetId(lot, lotId);

        var image = LotImage.Create(otherLotId, "photo.jpg", "lots/other/photo.jpg", "image/jpeg", 1024);
        SetId(image, imageId);

        var storage = Substitute.For<IImageStorageService>();
        var imageRepository = Substitute.For<ILotImageRepository>();
        var lotRepository = Substitute.For<ILotRepository>();

        lotRepository.GetByIdAsync(lotId, Arg.Any<CancellationToken>()).Returns(lot);
        imageRepository.GetByIdAsync(imageId, Arg.Any<CancellationToken>()).Returns(image);

        var endpoint = Factory.Create<DeleteLotImageEndpoint>(ctx =>
        {
            ctx.Request.RouteValues["lotId"] = lotId;
            ctx.Request.RouteValues["imageId"] = imageId;
            ctx.User = CreateUser(sellerId);
        }, storage, imageRepository, lotRepository);

        var act = () => endpoint.HandleAsync(CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationFailureException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        exception.Which.Failures.Should().ContainSingle(f => f.ErrorMessage == "Image not found");
        await storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        imageRepository.DidNotReceive().Remove(Arg.Any<LotImage>());
        await imageRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static ClaimsPrincipal CreateUser(Guid userId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "User")
        ], "Test");

        return new ClaimsPrincipal(identity);
    }

    private static void SetId(object entity, Guid id)
    {
        entity.GetType().GetProperty("Id")!.SetValue(entity, id);
    }
}
