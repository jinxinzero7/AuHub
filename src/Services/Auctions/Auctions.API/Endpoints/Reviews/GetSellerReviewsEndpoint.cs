using Auctions.Domain.Interfaces;
using FastEndpoints;

namespace Auctions.API.Endpoints.Reviews;

public class GetSellerReviewsEndpoint : EndpointWithoutRequest<SellerReviewsResponse>
{
    private readonly IReviewRepository _reviewRepository;

    public GetSellerReviewsEndpoint(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public override void Configure()
    {
        Get("/api/sellers/{sellerId}/reviews");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get seller reviews";
            s.Description = "Returns seller review list and aggregated rating.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sellerId = Route<Guid>("sellerId");
        var reviews = await _reviewRepository.GetBySellerIdAsync(sellerId, ct);
        var averageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(review => review.Rating), 2);

        Response = new SellerReviewsResponse
        {
            SellerId = sellerId,
            ReviewsCount = reviews.Count,
            AverageRating = averageRating,
            Reviews = reviews.Select(ReviewResponse.From).ToList()
        };
    }
}

public record SellerReviewsResponse
{
    public Guid SellerId { get; init; }
    public int ReviewsCount { get; init; }
    public double AverageRating { get; init; }
    public List<ReviewResponse> Reviews { get; init; } = new();
}
