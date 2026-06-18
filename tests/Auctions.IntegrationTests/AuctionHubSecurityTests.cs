using System.Security.Claims;
using Auctions.API.Hubs;
using Auctions.API.SignalR;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using AuHub.Shared.ValueObjects;
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
    public async Task JoinLotGroup_ActiveLot_AddsConnection()
    {
        var lot = CreateLot(active: true);
        var groups = Substitute.For<IGroupManager>();
        var lots = Substitute.For<ILotRepository>();
        lots.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        var hub = CreateHub(Guid.NewGuid().ToString(), groups, lots);

        await hub.JoinLotGroup(lot.Id);

        await groups.Received(1).AddToGroupAsync("connection-1", $"lot-{lot.Id}");
    }

    [Fact]
    public async Task JoinLotGroup_DraftLot_IsRejected()
    {
        var lot = CreateLot(active: false);
        var groups = Substitute.For<IGroupManager>();
        var lots = Substitute.For<ILotRepository>();
        lots.GetByIdAsync(lot.Id, Arg.Any<CancellationToken>()).Returns(lot);
        var hub = CreateHub(Guid.NewGuid().ToString(), groups, lots);

        var act = () => hub.JoinLotGroup(lot.Id);

        await act.Should().ThrowAsync<HubException>().WithMessage("Lot is not publicly available");
        await groups.DidNotReceiveWithAnyArgs().AddToGroupAsync(default!, default!);
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

    private static AuctionHub CreateHub(
        string? userId,
        IGroupManager groups,
        ILotRepository? lots = null)
    {
        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("connection-1");
        context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            userId is null ? [] : [new Claim(ClaimTypes.NameIdentifier, userId)],
            "test")));

        return new AuctionHub(lots ?? Substitute.For<ILotRepository>())
        {
            Context = context,
            Groups = groups
        };
    }

    private static Lot CreateLot(bool active)
    {
        var lot = Lot.Create(
            "Realtime lot",
            "Description",
            Money.FromDecimal(1000m),
            TimeSpan.FromHours(24),
            Guid.NewGuid(),
            [DeliveryProvider.Cdek]);

        if (active)
        {
            lot.SubmitForModeration();
            lot.Approve();
        }

        return lot;
    }
}
