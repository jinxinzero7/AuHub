using System.Security.Claims;
using Auctions.API.Hubs;
using Auctions.API.SignalR;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace Auctions.IntegrationTests;

public class AuctionHubSecurityTests
{
    [Fact]
    public async Task JoinUserGroup_UsesAuthenticatedUserClaim()
    {
        var userId = Guid.NewGuid();
        var groups = Substitute.For<IGroupManager>();
        var hub = CreateHub(userId.ToString(), groups);

        await hub.JoinUserGroup();

        await groups.Received(1).AddToGroupAsync("connection-1", $"user-{userId}");
    }

    [Fact]
    public async Task JoinUserGroup_WithMissingUserClaim_IsRejected()
    {
        var groups = Substitute.For<IGroupManager>();
        var hub = CreateHub(null, groups);

        var act = () => hub.JoinUserGroup();

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("Authenticated user identifier is missing");
        await groups.DidNotReceiveWithAnyArgs().AddToGroupAsync(default!, default!);
    }

    [Fact]
    public void Hub_DoesNotExposeClientCallableBroadcastMethods()
    {
        var publicMethods = typeof(AuctionHub).GetMethods()
            .Where(method => method.DeclaringType == typeof(AuctionHub))
            .Select(method => method.Name);

        publicMethods.Should().NotContain(["NewBidPlaced", "LotCompleted"]);
    }

    [Fact]
    public async Task PublishNewBid_UsesCurrentPriceContract()
    {
        var lotId = Guid.Parse("00000000-0000-0000-0000-000000000042");
        var clients = Substitute.For<IHubClients>();
        var proxy = Substitute.For<IClientProxy>();
        clients.Group($"lot-{lotId}").Returns(proxy);
        var hubContext = Substitute.For<IHubContext<AuctionHub>>();
        hubContext.Clients.Returns(clients);
        var publisher = new SignalREventPublisher(hubContext);

        await publisher.PublishNewBidAsync(lotId, 1250m, "Bidder");

        var call = proxy.ReceivedCalls().Single(received => received.GetMethodInfo().Name == "SendCoreAsync");
        call.GetArguments()[0].Should().Be("NewBidPlaced");

        var arguments = call.GetArguments()[1].Should().BeAssignableTo<object[]>().Subject;
        arguments.Should().ContainSingle();
        var payload = arguments[0];
        payload.GetType().GetProperty("currentPrice")!.GetValue(payload).Should().Be(1250m);
        payload.GetType().GetProperty("newPrice").Should().BeNull();
    }

    private static AuctionHub CreateHub(string? userId, IGroupManager groups)
    {
        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("connection-1");
        context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            userId is null ? [] : [new Claim(ClaimTypes.NameIdentifier, userId)],
            "test")));

        return new AuctionHub
        {
            Context = context,
            Groups = groups
        };
    }
}
