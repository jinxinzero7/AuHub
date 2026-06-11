using System.Security.Claims;
using Auctions.API.Endpoints.Reviews;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using AuHub.Shared.ValueObjects;
using FastEndpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Auctions.IntegrationTests;

public class CreateLotReviewEndpointTests
{
    [Fact]
    public async Task HandleAsync_WinnerAndCompletedTransaction_CreatesReview()
    {
        var sellerId = Guid.NewGuid();
        var winnerId = Guid.NewGuid();
        var lot = CreateReviewedLot(sellerId, winnerId, LotStatus.TransactionComplete);
        var lotRepository = Substitute.For<ILotRepository>();
        var reviewRepository = Substitute.For<IReviewRepository>();

        lotRepository.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        reviewRepository.GetByLotIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns((Review?)null);

        var endpoint = CreateEndpoint(lot.Id, winnerId, lotRepository, reviewRepository);
        var request = new CreateLotReviewRequest { Rating = 5, Comment = "Accurate description" };

        await endpoint.HandleAsync(request, CancellationToken.None);

        await reviewRepository.Received(1).AddAsync(
            Arg.Is<Review>(review =>
                review.LotId == lot.Id &&
                review.SellerId == sellerId &&
                review.BuyerId == winnerId &&
                review.Rating == 5),
            Arg.Any<CancellationToken>());
        await reviewRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        endpoint.Response.Rating.Should().Be(5);
    }

    [Fact]
    public async Task HandleAsync_UserIsNotWinner_ReturnsForbidden()
    {
        var sellerId = Guid.NewGuid();
        var winnerId = Guid.NewGuid();
        var lot = CreateReviewedLot(sellerId, winnerId, LotStatus.TransactionComplete);
        var lotRepository = Substitute.For<ILotRepository>();
        var reviewRepository = Substitute.For<IReviewRepository>();

        lotRepository.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);

        var endpoint = CreateEndpoint(lot.Id, Guid.NewGuid(), lotRepository, reviewRepository);

        var exception = await AssertEndpointError(endpoint, new CreateLotReviewRequest { Rating = 5 });

        exception.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        exception.Failures.Should().ContainSingle(f => f.ErrorMessage == "Only winner can review this lot");
        await reviewRepository.DidNotReceive().AddAsync(Arg.Any<Review>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_TransactionIsNotComplete_ReturnsBadRequest()
    {
        var sellerId = Guid.NewGuid();
        var winnerId = Guid.NewGuid();
        var lot = CreateReviewedLot(sellerId, winnerId, LotStatus.Delivered);
        var lotRepository = Substitute.For<ILotRepository>();
        var reviewRepository = Substitute.For<IReviewRepository>();

        lotRepository.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);

        var endpoint = CreateEndpoint(lot.Id, winnerId, lotRepository, reviewRepository);

        var exception = await AssertEndpointError(endpoint, new CreateLotReviewRequest { Rating = 5 });

        exception.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        exception.Failures.Should().ContainSingle(f => f.ErrorMessage == "Only completed transactions can be reviewed");
        await reviewRepository.DidNotReceive().AddAsync(Arg.Any<Review>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_LotAlreadyReviewed_ReturnsConflict()
    {
        var sellerId = Guid.NewGuid();
        var winnerId = Guid.NewGuid();
        var lot = CreateReviewedLot(sellerId, winnerId, LotStatus.TransactionComplete);
        var existingReview = Review.Create(lot.Id, sellerId, winnerId, 4, null);
        var lotRepository = Substitute.For<ILotRepository>();
        var reviewRepository = Substitute.For<IReviewRepository>();

        lotRepository.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        reviewRepository.GetByLotIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(existingReview);

        var endpoint = CreateEndpoint(lot.Id, winnerId, lotRepository, reviewRepository);

        var exception = await AssertEndpointError(endpoint, new CreateLotReviewRequest { Rating = 5 });

        exception.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        exception.Failures.Should().ContainSingle(f => f.ErrorMessage == "Lot already has a review");
        await reviewRepository.DidNotReceive().AddAsync(Arg.Any<Review>(), Arg.Any<CancellationToken>());
    }

    private static CreateLotReviewEndpoint CreateEndpoint(
        Guid lotId,
        Guid userId,
        ILotRepository lotRepository,
        IReviewRepository reviewRepository)
    {
        return Factory.Create<CreateLotReviewEndpoint>(ctx =>
        {
            ctx.Request.RouteValues["id"] = lotId;
            ctx.User = CreateUser(userId);
        }, lotRepository, reviewRepository);
    }

    private static async Task<ValidationFailureException> AssertEndpointError(
        CreateLotReviewEndpoint endpoint,
        CreateLotReviewRequest request)
    {
        var act = () => endpoint.HandleAsync(request, CancellationToken.None);
        var exception = await act.Should().ThrowAsync<ValidationFailureException>();
        return exception.Which;
    }

    private static Lot CreateReviewedLot(Guid sellerId, Guid winnerId, LotStatus status)
    {
        var lot = Lot.Create("Lot", "Description", Money.FromDecimal(100m), TimeSpan.FromDays(1), sellerId, [DeliveryProvider.Cdek]);
        SetProperty(lot, nameof(Lot.WinnerId), winnerId);
        SetProperty(lot, nameof(Lot.Status), status);
        return lot;
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

    private static void SetProperty<T>(object entity, string propertyName, T value)
    {
        entity.GetType().GetProperty(propertyName)!.SetValue(entity, value);
    }
}
