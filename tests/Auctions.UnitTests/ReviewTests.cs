using Auctions.Domain.Entities;
using FluentAssertions;

namespace Auctions.UnitTests;

public class ReviewTests
{
    [Fact]
    public void Create_ValidData_SetsProperties()
    {
        var lotId = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();

        var review = Review.Create(lotId, sellerId, buyerId, 5, "  Great seller  ");

        review.LotId.Should().Be(lotId);
        review.SellerId.Should().Be(sellerId);
        review.BuyerId.Should().Be(buyerId);
        review.Rating.Should().Be(5);
        review.Comment.Should().Be("Great seller");
        review.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_InvalidRating_Throws(int rating)
    {
        var act = () => Review.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), rating, null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Rating must be between 1 and 5");
    }

    [Fact]
    public void Create_BlankComment_StoresNull()
    {
        var review = Review.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 4, "   ");

        review.Comment.Should().BeNull();
    }

    [Fact]
    public void Create_TooLongComment_Throws()
    {
        var act = () => Review.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 4, new string('a', 1001));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Review comment is too long");
    }
}
