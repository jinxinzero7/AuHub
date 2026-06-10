using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Events;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using System.Reflection;

namespace Auctions.UnitTests;

public class LotTests
{
    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid BidderId = Guid.NewGuid();

    private static Lot CreateValidLot()
    {
        return Lot.Create(
            "Test Lot",
            "Description",
            Money.FromDecimal(1000m),
            TimeSpan.FromDays(3),
            SellerId,
            [DeliveryProvider.Cdek, DeliveryProvider.RussianPost]);
    }

    private static Lot CreateActiveLot()
    {
        var lot = CreateValidLot();
        lot.SubmitForModeration();
        lot.Approve();
        return lot;
    }

    private static void PlaceBidAndAttach(Lot lot, Money amount, Guid bidderId, string bidderName)
    {
        lot.PlaceBid(amount, bidderId, bidderName);
        var bidsField = typeof(Lot).GetField("_bids", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var bids = (List<Bid>)bidsField.GetValue(lot)!;
        bids.Add(Bid.Create(lot.Id, bidderId, amount));
    }

    private static void SetDeliveryRequestDeadline(Lot lot, DateTime deadline)
    {
        typeof(Lot).GetProperty(nameof(Lot.DeliveryRequestDeadlineAt))!
            .SetValue(lot, deadline);
    }

    [Fact]
    public void Create_SetsProperties()
    {
        var lot = CreateValidLot();

        lot.Title.Should().Be("Test Lot");
        lot.Description.Should().Be("Description");
        lot.StartingPrice.Should().Be(Money.FromDecimal(1000m));
        lot.CurrentPrice.Should().Be(Money.FromDecimal(1000m));
        lot.SellerId.Should().Be(SellerId);
        lot.Status.Should().Be(LotStatus.Draft);
        lot.SupportedDeliveryProviders.Should().BeEquivalentTo([DeliveryProvider.Cdek, DeliveryProvider.RussianPost]);
        lot.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithoutDeliveryProviders_Throws()
    {
        var act = () => Lot.Create("Test Lot", "Description", Money.FromDecimal(1000m), TimeSpan.FromDays(3), SellerId, []);
        act.Should().Throw<InvalidOperationException>().WithMessage("At least one delivery provider is required");
    }

    [Fact]
    public void Create_InitializesEmptyCollections()
    {
        var lot = CreateValidLot();
        lot.Bids.Should().BeEmpty();
        lot.Images.Should().BeEmpty();
    }

    // --- Status transitions ---

    [Fact]
    public void SubmitForModeration_TransitionsDraftToPendingModeration()
    {
        var lot = CreateValidLot();
        lot.SubmitForModeration();
        lot.Status.Should().Be(LotStatus.PendingModeration);
        lot.StartTime.Should().BeNull();
        lot.EndTime.Should().BeNull();
    }

    [Fact]
    public void SubmitForModeration_NonDraft_Throws()
    {
        var lot = CreateValidLot();
        lot.SubmitForModeration();
        var act = () => lot.SubmitForModeration();
        act.Should().Throw<InvalidOperationException>().WithMessage("Only draft lots can be submitted for moderation");
    }

    [Fact]
    public void Approve_TransitionsPendingModerationToActive()
    {
        var lot = CreateValidLot();
        lot.SubmitForModeration();
        lot.Approve();
        lot.Status.Should().Be(LotStatus.Active);
        lot.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        lot.EndTime.Should().Be(lot.StartTime!.Value.Add(TimeSpan.FromDays(3)));
    }

    [Fact]
    public void Approve_NonPendingModeration_Throws()
    {
        var lot = CreateValidLot();
        var act = () => lot.Approve();
        act.Should().Throw<InvalidOperationException>().WithMessage("Only lots pending moderation can be approved");
    }

    [Fact]
    public void Reject_SetsStatusAndComment()
    {
        var lot = CreateValidLot();
        lot.SubmitForModeration();
        lot.Reject("Bad quality");
        lot.Status.Should().Be(LotStatus.Rejected);
        lot.AdminComment.Should().Be("Bad quality");
    }

    [Fact]
    public void Reject_NonDraft_Throws()
    {
        var lot = CreateValidLot();
        var act = () => lot.Reject("reason");
        act.Should().Throw<InvalidOperationException>().WithMessage("Only lots pending moderation can be rejected");
    }

    [Fact]
    public void Publish_TransitionsDraftToPendingModeration()
    {
        var lot = CreateValidLot();
        lot.Publish();
        lot.Status.Should().Be(LotStatus.PendingModeration);
    }

    // --- PlaceBid ---

    [Fact]
    public void PlaceBid_WithValidBid_UpdatesCurrentPrice()
    {
        var lot = CreateActiveLot();
        lot.PlaceBid(Money.FromDecimal(1500m), BidderId, "Bidder");

        lot.CurrentPrice.Should().Be(Money.FromDecimal(1500m));
    }

    [Fact]
    public void PlaceBid_WhenLotNotActive_Throws()
    {
        var lot = CreateValidLot();
        var act = () => lot.PlaceBid(Money.FromDecimal(1500m), BidderId, "Bidder");
        act.Should().Throw<InvalidOperationException>().WithMessage("Lot is not active");
    }

    [Fact]
    public void PlaceBid_WhenSellerBids_Throws()
    {
        var lot = CreateActiveLot();
        var act = () => lot.PlaceBid(Money.FromDecimal(1500m), SellerId, "Seller");
        act.Should().Throw<InvalidOperationException>().WithMessage("Seller cannot bid on own lot");
    }

    [Fact]
    public void PlaceBid_WhenAmountTooLow_Throws()
    {
        var lot = CreateActiveLot();
        var act = () => lot.PlaceBid(Money.FromDecimal(500m), BidderId, "Bidder");
        act.Should().Throw<InvalidOperationException>().WithMessage("Bid amount must be higher than current price");
    }

    [Fact]
    public void Complete_WithoutBids_SetsCompletedNoWinner()
    {
        var lot = CreateActiveLot();
        lot.Complete();
        lot.Status.Should().Be(LotStatus.CompletedNoWinner);
        lot.WinnerId.Should().BeNull();
    }

    [Fact]
    public void PlaceBid_WhenAuctionEnded_Throws()
    {
        var lot = CreateActiveLot();
        lot.ExtendEndTime(-TimeSpan.FromDays(3));
        var act = () => lot.PlaceBid(Money.FromDecimal(1500m), BidderId, "Bidder");
        act.Should().Throw<InvalidOperationException>().WithMessage("Auction has ended");
    }

    [Fact]
    public void PlaceBid_RaisesDomainEvent()
    {
        var lot = CreateActiveLot();
        lot.PlaceBid(Money.FromDecimal(1500m), BidderId, "Bidder");

        lot.DomainEvents.Should().ContainSingle(e => e is BidPlacedDomainEvent);
        var evt = lot.DomainEvents.OfType<BidPlacedDomainEvent>().Single();
        evt.LotId.Should().Be(lot.Id);
        evt.BidderId.Should().Be(BidderId);
        evt.Amount.Should().Be(1500m);
        evt.SellerId.Should().Be(SellerId);
    }

    // --- Sniper Protection ---

    [Fact]
    public void ExtendEndTime_AddsExtension()
    {
        var lot = CreateActiveLot();
        var originalEnd = lot.EndTime!.Value;
        lot.ExtendEndTime(TimeSpan.FromMinutes(2));
        lot.EndTime.Should().Be(originalEnd.Add(TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void ExtendEndTime_WhenNoEndTime_DoesNothing()
    {
        var lot = CreateValidLot();
        lot.ExtendEndTime(TimeSpan.FromMinutes(2));
        lot.EndTime.Should().BeNull();
    }

    [Fact]
    public void ApplySniperProtection_WhenInsideWindow_ExtendsEndTime()
    {
        var lot = CreateActiveLot();
        lot.ExtendEndTime(-lot.Duration + TimeSpan.FromSeconds(25));
        var originalEnd = lot.EndTime!.Value;

        var applied = lot.ApplySniperProtection(originalEnd.AddSeconds(-25));

        applied.Should().BeTrue();
        lot.EndTime.Should().Be(originalEnd.Add(Lot.SniperProtectionExtension));
    }

    [Fact]
    public void ApplySniperProtection_WhenMaxExtensionReached_DoesNotExtend()
    {
        var lot = CreateActiveLot();
        var initialEnd = lot.EndTime!.Value;
        lot.ExtendEndTime(Lot.MaxSniperProtectionExtension);

        var applied = lot.ApplySniperProtection(lot.EndTime!.Value.AddSeconds(-25));

        applied.Should().BeFalse();
        lot.EndTime.Should().Be(initialEnd.Add(Lot.MaxSniperProtectionExtension));
    }

    [Fact]
    public void ApplySniperProtection_WhenExtensionWouldExceedMax_CapsEndTime()
    {
        var lot = CreateActiveLot();
        var maxEndTime = lot.EndTime!.Value.Add(Lot.MaxSniperProtectionExtension);
        lot.ExtendEndTime(Lot.MaxSniperProtectionExtension - TimeSpan.FromSeconds(30));

        var applied = lot.ApplySniperProtection(lot.EndTime!.Value.AddSeconds(-25));

        applied.Should().BeTrue();
        lot.EndTime.Should().Be(maxEndTime);
    }

    // --- Complete ---

    [Fact]
    public void Complete_SetsStatusAndWinner()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        lot.Status.Should().Be(LotStatus.Completed);
        lot.WinnerId.Should().Be(BidderId);
    }

    [Fact]
    public void Complete_WhenNotActive_Throws()
    {
        var lot = CreateValidLot();
        var act = () => lot.Complete();
        act.Should().Throw<InvalidOperationException>().WithMessage("Only active lots can be completed");
    }

    [Fact]
    public void Complete_RaisesDomainEvent()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.ClearDomainEvents();
        lot.Complete("WinnerName");

        lot.DomainEvents.Should().ContainSingle(e => e is AuctionCompletedDomainEvent);
        var evt = lot.DomainEvents.OfType<AuctionCompletedDomainEvent>().Single();
        evt.LotId.Should().Be(lot.Id);
        evt.FinalPrice.Should().Be(1500m);
    }

    [Fact]
    public void OpenDeliveryRequestWindow_Completed_SetsDeliveryRequestPending()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        lot.OpenDeliveryRequestWindow();
        lot.Status.Should().Be(LotStatus.DeliveryRequestPending);
        lot.DeliveryRequestDeadlineAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(3), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void OpenDeliveryRequestWindow_CompletedWithoutWinner_Throws()
    {
        var lot = CreateActiveLot();
        lot.Complete();

        var act = () => lot.OpenDeliveryRequestWindow();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only completed lots can open delivery request window");
    }

    [Fact]
    public void RequestDelivery_PendingDeliveryRequest_SetsDeliveryDetailsAndShippingPending()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        lot.OpenDeliveryRequestWindow();
        lot.RequestDelivery(DeliveryProvider.Cdek, "PVZ address", "Test Recipient", "+79990000000");
        lot.Status.Should().Be(LotStatus.ShippingPending);
        lot.SelectedDeliveryProvider.Should().Be(DeliveryProvider.Cdek);
        lot.DeliveryAddress.Should().Be("PVZ address");
        lot.DeliveryRecipientName.Should().Be("Test Recipient");
        lot.DeliveryRecipientPhone.Should().Be("+79990000000");
        lot.DeliveryRequestedAt.Should().NotBeNull();
    }

    [Fact]
    public void RequestDelivery_UnsupportedProvider_Throws()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        lot.OpenDeliveryRequestWindow();

        var act = () => lot.RequestDelivery(
            DeliveryProvider.YandexDelivery,
            "PVZ address",
            "Test Recipient",
            "+79990000000");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Delivery provider is not supported for this lot");
    }

    [Fact]
    public void RequestDelivery_AfterDeadline_Throws()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        lot.OpenDeliveryRequestWindow();
        SetDeliveryRequestDeadline(lot, DateTime.UtcNow.AddSeconds(-1));

        var act = () => lot.RequestDelivery(
            DeliveryProvider.Cdek,
            "PVZ address",
            "Test Recipient",
            "+79990000000");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Delivery request deadline has expired");
    }

    [Fact]
    public void ExpireDeliveryRequest_PendingDeliveryRequest_SetsExpired()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        lot.OpenDeliveryRequestWindow();
        lot.ExpireDeliveryRequest();
        lot.Status.Should().Be(LotStatus.DeliveryRequestExpired);
    }

    [Fact]
    public void Ship_ShippingPending_SetsTrackingNumberAndShipped()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        lot.OpenDeliveryRequestWindow();
        lot.RequestDelivery(DeliveryProvider.Cdek, "PVZ address", "Test Recipient", "+79990000000");

        lot.Ship("TRACK-1");

        lot.Status.Should().Be(LotStatus.Shipped);
        lot.TrackingNumber.Should().Be("TRACK-1");
    }

    [Fact]
    public void CompleteTransaction_Delivered_SetsTransactionComplete()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        lot.OpenDeliveryRequestWindow();
        lot.RequestDelivery(DeliveryProvider.Cdek, "PVZ address", "Test Recipient", "+79990000000");
        lot.MarkShipped("TRACK-1");
        lot.ConfirmDelivery();
        lot.CompleteTransaction();
        lot.Status.Should().Be(LotStatus.TransactionComplete);
    }

    // --- Cancel ---

    [Fact]
    public void Cancel_Draft_ChangesToCancelled()
    {
        var lot = CreateValidLot();
        lot.Cancel();
        lot.Status.Should().Be(LotStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var lot = CreateValidLot();
        lot.Cancel();
        var act = () => lot.Cancel();
        act.Should().Throw<InvalidOperationException>().WithMessage("Lot is already cancelled");
    }

    [Fact]
    public void Cancel_ActiveWithoutBids_Succeeds()
    {
        var lot = CreateActiveLot();
        lot.Cancel();
        lot.Status.Should().Be(LotStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Completed_Throws()
    {
        var lot = CreateActiveLot();
        lot.PlaceBid(Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        var act = () => lot.Cancel();
        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot cancel finalized lot");
    }

    // --- Freeze / Unfreeze ---

    [Fact]
    public void Freeze_Active_SetsFrozen()
    {
        var lot = CreateActiveLot();
        lot.Freeze();
        lot.Status.Should().Be(LotStatus.Frozen);
    }

    [Fact]
    public void Freeze_NonActive_Throws()
    {
        var lot = CreateValidLot();
        var act = () => lot.Freeze();
        act.Should().Throw<InvalidOperationException>().WithMessage("Only active lots can be frozen");
    }

    [Fact]
    public void Unfreeze_RestoresActive()
    {
        var lot = CreateActiveLot();
        lot.Freeze();
        lot.Unfreeze();
        lot.Status.Should().Be(LotStatus.Active);
    }

    // --- SoftDelete ---

    [Fact]
    public void SoftDelete_MarksAsDeleted()
    {
        var lot = CreateValidLot();
        lot.SoftDelete();
        lot.IsDeleted.Should().BeTrue();
        lot.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SoftDelete_SetsDeletedBy()
    {
        var adminId = Guid.NewGuid();
        var lot = CreateValidLot();
        lot.SoftDelete(adminId);
        lot.DeletedBy.Should().Be(adminId);
    }

    [Fact]
    public void SoftDelete_AlreadyDeleted_Throws()
    {
        var lot = CreateValidLot();
        lot.SoftDelete();
        var act = () => lot.SoftDelete();
        act.Should().Throw<InvalidOperationException>().WithMessage("Lot is already deleted");
    }

    // --- Images ---

    [Fact]
    public void AddImage_AddsToCollection()
    {
        var lot = CreateValidLot();
        var image = LotImage.Create(lot.Id, "photo.jpg", "lots/abc/photo.jpg", "image/jpeg", 1024);
        lot.AddImage(image);
        lot.Images.Should().ContainSingle();
    }

    [Fact]
    public void RemoveImage_Existing_RemovesAndReturnsTrue()
    {
        var lot = CreateValidLot();
        var image = LotImage.Create(lot.Id, "photo.jpg", "lots/abc/photo.jpg", "image/jpeg", 1024);
        lot.AddImage(image);
        var result = lot.RemoveImage(image.Id);
        result.Should().BeTrue();
        lot.Images.Should().BeEmpty();
    }

    [Fact]
    public void RemoveImage_NonExisting_ReturnsFalse()
    {
        var lot = CreateValidLot();
        var result = lot.RemoveImage(Guid.NewGuid());
        result.Should().BeFalse();
    }

    // --- Domain Events ---

    [Fact]
    public void ClearDomainEvents_EmptiesCollection()
    {
        var lot = CreateActiveLot();
        lot.PlaceBid(Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.ClearDomainEvents();
        lot.DomainEvents.Should().BeEmpty();
    }

    // --- Dispute ---

    [Fact]
    public void OpenDispute_SetsDisputed()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        lot.OpenDispute("Item not as described");
        lot.Status.Should().Be(LotStatus.Disputed);
        lot.DisputeReason.Should().Be("Item not as described");
    }

    [Fact]
    public void ResolveDispute_InFavorOfBuyer_Cancels()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        lot.OpenDispute("broken");
        lot.ResolveDispute(true);
        lot.Status.Should().Be(LotStatus.Cancelled);
    }

    [Fact]
    public void ResolveDispute_InFavorOfSeller_Completes()
    {
        var lot = CreateActiveLot();
        PlaceBidAndAttach(lot, Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.Complete();
        lot.OpenDispute("broken");
        lot.ResolveDispute(false);
        lot.Status.Should().Be(LotStatus.TransactionComplete);
    }
}
