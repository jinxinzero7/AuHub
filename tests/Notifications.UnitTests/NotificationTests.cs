using FluentAssertions;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;

namespace Notifications.UnitTests;

public class NotificationTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_SetsProperties()
    {
        var notification = Notification.Create(UserId, NotificationType.NewBid, "New Bid", "You received a new bid");

        notification.Id.Should().NotBeEmpty();
        notification.UserId.Should().Be(UserId);
        notification.Type.Should().Be(NotificationType.NewBid);
        notification.Title.Should().Be("New Bid");
        notification.Message.Should().Be("You received a new bid");
        notification.IsRead.Should().BeFalse();
        notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithDifferentTypes_SetsType()
    {
        var types = new[]
        {
            NotificationType.NewBid,
            NotificationType.Outbid,
            NotificationType.WonAuction,
            NotificationType.LotCompleted,
            NotificationType.AuctionEndingSoon,
            NotificationType.LotApproved,
            NotificationType.LotRejected,
            NotificationType.LotFrozen,
            NotificationType.DisputeResolved
        };

        foreach (var type in types)
        {
            var notification = Notification.Create(UserId, type, "Test", "Test");
            notification.Type.Should().Be(type);
        }
    }

    [Fact]
    public void MarkAsRead_SetsIsReadToTrue()
    {
        var notification = Notification.Create(UserId, NotificationType.NewBid, "Title", "Message");

        notification.MarkAsRead();

        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_DoesNotThrow()
    {
        var notification = Notification.Create(UserId, NotificationType.NewBid, "Title", "Message");
        notification.MarkAsRead();

        var act = () => notification.MarkAsRead();

        act.Should().NotThrow();
        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyUserId_StillCreates()
    {
        var notification = Notification.Create(Guid.Empty, NotificationType.NewBid, "Title", "Message");
        notification.UserId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var n1 = Notification.Create(UserId, NotificationType.NewBid, "Title", "Message");
        var n2 = Notification.Create(UserId, NotificationType.NewBid, "Title", "Message");

        n1.Id.Should().NotBe(n2.Id);
    }
}
