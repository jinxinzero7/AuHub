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

public class LotImageMutationEndpointTests
{
    [Theory]
    [InlineData(LotStatus.PendingModeration)]
    [InlineData(LotStatus.Active)]
    public async Task UploadImage_NonEditableLot_ReturnsConflictWithoutStorageMutation(LotStatus status)
    {
        var context = CreateContext(status);
        var endpoint = CreateUploadEndpoint(context);

        var act = () => endpoint.HandleAsync(CancellationToken.None);

        await AssertStateConflictAsync(act);
        await context.Storage.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await context.Images.DidNotReceive().AddAsync(Arg.Any<LotImage>(), Arg.Any<CancellationToken>());
        await context.Images.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(LotStatus.PendingModeration)]
    [InlineData(LotStatus.Active)]
    public async Task DeleteImage_NonEditableLot_ReturnsConflictWithoutStorageMutation(LotStatus status)
    {
        var context = CreateContext(status);
        var image = CreateImage(context.Lot.Id);
        var endpoint = CreateDeleteEndpoint(context, image);

        var act = () => endpoint.HandleAsync(CancellationToken.None);

        await AssertStateConflictAsync(act);
        await context.Images.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await context.Storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        context.Images.DidNotReceive().Remove(Arg.Any<LotImage>());
    }

    [Theory]
    [InlineData(LotStatus.Draft)]
    [InlineData(LotStatus.Rejected)]
    public async Task UploadImage_EditableLot_UploadsAndPersistsImage(LotStatus status)
    {
        var context = CreateContext(status);
        context.Storage.UploadAsync(
                Arg.Any<Stream>(), Arg.Any<string>(), "image/jpeg", Arg.Any<CancellationToken>())
            .Returns("stored");
        context.Storage.GetPresignedUrlAsync(Arg.Any<string>(), 60, Arg.Any<CancellationToken>())
            .Returns("https://storage.test/image");
        var endpoint = CreateUploadEndpoint(context, includeFile: true);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        await context.Storage.Received(1).UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), "image/jpeg", Arg.Any<CancellationToken>());
        await context.Images.Received(1).AddAsync(
            Arg.Is<LotImage>(image => image.LotId == context.Lot.Id), Arg.Any<CancellationToken>());
        await context.Images.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(LotStatus.Draft)]
    [InlineData(LotStatus.Rejected)]
    public async Task DeleteImage_EditableLot_DeletesFromStorageAndRepository(LotStatus status)
    {
        var context = CreateContext(status);
        var image = CreateImage(context.Lot.Id);
        context.Images.GetByIdAsync(image.Id, Arg.Any<CancellationToken>()).Returns(image);
        var endpoint = CreateDeleteEndpoint(context, image);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        await context.Storage.Received(1).DeleteAsync(image.ObjectName, Arg.Any<CancellationToken>());
        context.Images.Received(1).Remove(image);
        await context.Images.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static UploadLotImageEndpoint CreateUploadEndpoint(TestContext context, bool includeFile = false)
    {
        return Factory.Create<UploadLotImageEndpoint>(httpContext =>
        {
            httpContext.Request.RouteValues["id"] = context.Lot.Id;
            httpContext.User = CreateUser(context.Lot.SellerId);
            if (includeFile)
            {
                var file = new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "files", "photo.jpg")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/jpeg"
                };
                httpContext.Request.Form = new FormCollection([], new FormFileCollection { file });
            }
        }, context.Storage, context.Images, context.Lots);
    }

    private static DeleteLotImageEndpoint CreateDeleteEndpoint(TestContext context, LotImage image)
    {
        return Factory.Create<DeleteLotImageEndpoint>(httpContext =>
        {
            httpContext.Request.RouteValues["lotId"] = context.Lot.Id;
            httpContext.Request.RouteValues["imageId"] = image.Id;
            httpContext.User = CreateUser(context.Lot.SellerId);
        }, context.Storage, context.Images, context.Lots);
    }

    private static TestContext CreateContext(LotStatus status)
    {
        var sellerId = Guid.NewGuid();
        var lot = Lot.Create(
            "Lot", "Description", Money.FromDecimal(100m), TimeSpan.FromDays(1), sellerId, [DeliveryProvider.Cdek]);
        MoveToStatus(lot, status);

        var storage = Substitute.For<IImageStorageService>();
        var images = Substitute.For<ILotImageRepository>();
        var lots = Substitute.For<ILotRepository>();
        lots.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        return new TestContext(lot, storage, images, lots);
    }

    private static void MoveToStatus(Lot lot, LotStatus status)
    {
        if (status == LotStatus.Draft)
            return;

        lot.SubmitForModeration();
        if (status == LotStatus.Rejected)
            lot.Reject("Needs changes");
        else if (status == LotStatus.Active)
            lot.Approve();
    }

    private static LotImage CreateImage(Guid lotId) =>
        LotImage.Create(lotId, "photo.jpg", $"lots/{lotId}/photo.jpg", "image/jpeg", 3);

    private static ClaimsPrincipal CreateUser(Guid userId) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Role, "User")
    ], "Test"));

    private static async Task AssertStateConflictAsync(Func<Task> act)
    {
        var exception = await act.Should().ThrowAsync<ValidationFailureException>();
        exception.Which.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        exception.Which.Failures.Should().ContainSingle(f =>
            f.ErrorMessage == "Images can only be changed for draft or rejected lots");
    }

    private sealed record TestContext(
        Lot Lot,
        IImageStorageService Storage,
        ILotImageRepository Images,
        ILotRepository Lots);
}
