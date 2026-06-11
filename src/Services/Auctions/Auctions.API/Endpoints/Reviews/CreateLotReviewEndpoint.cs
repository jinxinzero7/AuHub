using System.Security.Claims;
using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;
using FastEndpoints;

namespace Auctions.API.Endpoints.Reviews;

public class CreateLotReviewEndpoint : Endpoint<CreateLotReviewRequest, ReviewResponse>
{
    private readonly ILotRepository _lotRepository;
    private readonly IReviewRepository _reviewRepository;

    public CreateLotReviewEndpoint(ILotRepository lotRepository, IReviewRepository reviewRepository)
    {
        _lotRepository = lotRepository;
        _reviewRepository = reviewRepository;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/reviews");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Create seller review";
            s.Description = "Create one review for a completed transaction. Only the winning buyer can review the seller.";
        });
    }

    public override async Task HandleAsync(CreateLotReviewRequest req, CancellationToken ct)
    {
        var lotId = Route<Guid>("id");
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID", 401);
            return;
        }

        var lot = await _lotRepository.GetByIdAsync(lotId, ct);
        if (lot == null)
        {
            ThrowError("Lot not found", 404);
            return;
        }

        if (lot.WinnerId != userId)
        {
            ThrowError("Only winner can review this lot", 403);
            return;
        }

        if (lot.Status != LotStatus.TransactionComplete)
        {
            ThrowError("Only completed transactions can be reviewed", 400);
            return;
        }

        var existingReview = await _reviewRepository.GetByLotIdAsync(lotId, ct);
        if (existingReview != null)
        {
            ThrowError("Lot already has a review", 409);
            return;
        }

        Review review;
        try
        {
            review = Review.Create(lot.Id, lot.SellerId, userId, req.Rating, req.Comment);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, 400);
            return;
        }

        await _reviewRepository.AddAsync(review, ct);
        await _reviewRepository.SaveChangesAsync(ct);

        Response = ReviewResponse.From(review);
    }
}

public record CreateLotReviewRequest
{
    public int Rating { get; init; }
    public string? Comment { get; init; }
}

public record ReviewResponse
{
    public Guid Id { get; init; }
    public Guid LotId { get; init; }
    public Guid SellerId { get; init; }
    public Guid BuyerId { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }

    public static ReviewResponse From(Review review)
    {
        return new ReviewResponse
        {
            Id = review.Id,
            LotId = review.LotId,
            SellerId = review.SellerId,
            BuyerId = review.BuyerId,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };
    }
}
