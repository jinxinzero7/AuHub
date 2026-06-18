using Identity.Application.Queries.GetAdminUserDetail;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace Identity.UnitTests;

public class GetAdminUserDetailQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_KnownUser_AggregatesSafeIdentityMetadata()
    {
        var user = User.Create("seller@example.com", "+79990000000", "seller", "hash", "Seller", UserRole.User);
        user.MarkEmailVerified();
        user.Ban("Review required");
        var request = DocumentVerificationRequest.Create(user.Id, "private/passport.jpg", "private/selfie.jpg");
        var users = Substitute.For<IUserRepository>();
        var documents = Substitute.For<IDocumentVerificationRequestRepository>();
        users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        documents.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns([request]);
        var handler = new GetAdminUserDetailQueryHandler(users, documents);

        var result = await handler.HandleAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("seller@example.com");
        result.Value.IsEmailVerified.Should().BeTrue();
        result.Value.IsBanned.Should().BeTrue();
        result.Value.DocumentVerificationHistory.Should().ContainSingle(metadata =>
            metadata.RequestId == request.Id && metadata.Status == "PendingReview");
    }

    [Fact]
    public async Task HandleAsync_UnknownUser_ReturnsNotFoundWithoutLoadingDocuments()
    {
        var users = Substitute.For<IUserRepository>();
        var documents = Substitute.For<IDocumentVerificationRequestRepository>();
        var handler = new GetAdminUserDetailQueryHandler(users, documents);

        var result = await handler.HandleAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        await documents.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
