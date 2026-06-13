using FluentAssertions;
using Identity.Domain.Entities;

namespace Identity.UnitTests;

public class DocumentVerificationRequestTests
{
    [Fact]
    public void Create_SetsPendingReviewState()
    {
        var userId = Guid.NewGuid();

        var request = DocumentVerificationRequest.Create(userId, "private/passport.jpg", "private/selfie.jpg");

        request.Id.Should().NotBeEmpty();
        request.UserId.Should().Be(userId);
        request.PassportImagePath.Should().Be("private/passport.jpg");
        request.SelfieImagePath.Should().Be("private/selfie.jpg");
        request.Status.Should().Be(DocumentVerificationRequestStatus.PendingReview);
        request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        request.ReviewedAt.Should().BeNull();
        request.ReviewedByAdminId.Should().BeNull();
        request.RejectionReason.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithoutPassportImagePath_Throws(string passportImagePath)
    {
        var act = () => DocumentVerificationRequest.Create(Guid.NewGuid(), passportImagePath, "private/selfie.jpg");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Passport image path is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithoutSelfieImagePath_Throws(string selfieImagePath)
    {
        var act = () => DocumentVerificationRequest.Create(Guid.NewGuid(), "private/passport.jpg", selfieImagePath);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Selfie image path is required");
    }

    [Fact]
    public void Approve_WhenPending_MarksApproved()
    {
        var adminId = Guid.NewGuid();
        var request = DocumentVerificationRequest.Create(Guid.NewGuid(), "private/passport.jpg", "private/selfie.jpg");

        request.Approve(adminId);

        request.Status.Should().Be(DocumentVerificationRequestStatus.Approved);
        request.ReviewedByAdminId.Should().Be(adminId);
        request.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        request.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        request.RejectionReason.Should().BeNull();
    }

    [Fact]
    public void Reject_WhenPending_MarksRejectedWithReason()
    {
        var adminId = Guid.NewGuid();
        var request = DocumentVerificationRequest.Create(Guid.NewGuid(), "private/passport.jpg", "private/selfie.jpg");

        request.Reject(adminId, "Bad image quality");

        request.Status.Should().Be(DocumentVerificationRequestStatus.Rejected);
        request.ReviewedByAdminId.Should().Be(adminId);
        request.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        request.RejectionReason.Should().Be("Bad image quality");
    }

    [Fact]
    public void Reject_WithoutReason_Throws()
    {
        var request = DocumentVerificationRequest.Create(Guid.NewGuid(), "private/passport.jpg", "private/selfie.jpg");

        var act = () => request.Reject(Guid.NewGuid(), " ");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Rejection reason is required");
    }

    [Fact]
    public void Review_WhenAlreadyReviewed_Throws()
    {
        var request = DocumentVerificationRequest.Create(Guid.NewGuid(), "private/passport.jpg", "private/selfie.jpg");
        request.Approve(Guid.NewGuid());

        var act = () => request.Reject(Guid.NewGuid(), "Changed mind");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only pending document verification requests can be reviewed");
    }
}
