using Auctions.API.Endpoints.Reviews;
using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;
using FastEndpoints;
using FluentAssertions;
using NSubstitute;

namespace Auctions.IntegrationTests;

public class GetSellerReviewsEndpointTests
{
    [Fact]
    public async Task HandleAsync_ReturnsReviewsAndAverageRating()
    {
        var sellerId = Guid.NewGuid();
        var reviews = new List<Review>
        {
            Review.Create(Guid.NewGuid(), sellerId, Guid.NewGuid(), 5, "Fast delivery"),
            Review.Create(Guid.NewGuid(), sellerId, Guid.NewGuid(), 4, "Good lot")
        };
        var reviewRepository = Substitute.For<IReviewRepository>();
        reviewRepository.GetBySellerIdAsync(sellerId, Arg.Any<CancellationToken>()).Returns(reviews);

        var endpoint = Factory.Create<GetSellerReviewsEndpoint>(ctx =>
        {
            ctx.Request.RouteValues["sellerId"] = sellerId;
        }, reviewRepository);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.Response.SellerId.Should().Be(sellerId);
        endpoint.Response.ReviewsCount.Should().Be(2);
        endpoint.Response.AverageRating.Should().Be(4.5);
        endpoint.Response.Reviews.Should().HaveCount(2);
    }
}
