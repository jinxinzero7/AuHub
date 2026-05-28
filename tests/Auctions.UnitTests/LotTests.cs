using Auctions.Domain.Entities;
using Auctions.Domain.Events;
using AuHub.Shared.ValueObjects;
using FluentAssertions;

namespace Auctions.UnitTests;

public class LotTests
{
    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid BidderId = Guid.NewGuid();

    private static Lot CreateValidLot()
    {
        return Lot.Create("Test Lot", "Description", Money.FromDecimal(1000m), TimeSpan.FromDays(3), SellerId);
    }

    private static Lot CreateActiveLot()
    {
        var lot = CreateValidLot();
        lot.Approve();
        return lot;
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
        lot.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
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
    public void Approve_TransitionsDraftToActive()
    {
        var lot = CreateValidLot();
        lot.Approve();
        lot.Status.Should().Be(LotStatus.Active);
        lot.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        lot.EndTime.Should().Be(lot.StartTime!.Value.Add(TimeSpan.FromDays(3)));
    }

    [Fact]
    public void Approve_NonDraft_Throws()
    {
        var lot = CreateValidLot();
        lot.Approve();
        var act = () => lot.Approve();
        act.Should().Throw<InvalidOperationException>().WithMessage("Only draft lots can be approved");
    }

    [Fact]
    public void Reject_SetsStatusAndComment()
    {
        var lot = CreateValidLot();
        lot.Reject("Bad quality");
        lot.Status.Should().Be(LotStatus.Rejected);
        lot.AdminComment.Should().Be("Bad quality");
    }

    [Fact]
    public void Reject_NonDraft_Throws()
    {
        var lot = CreateValidLot();
        lot.Approve();
        var act = () => lot.Reject("reason");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Publish_TransitionsApprovedToActive()
    {
        var lot = CreateValidLot();
        lot.GetType().GetProperty("Status")!.SetValue(lot, LotStatus.Approved);
        lot.Publish();

        lot.Status.Should().Be(LotStatus.Active);
        lot.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        lot.EndTime.Should().Be(lot.StartTime!.Value.Add(TimeSpan.FromDays(3)));
    }

    [Fact]
    public void Publish_NonApproved_Throws()
    {
        var lot = CreateValidLot();
        var act = () => lot.Publish();
        act.Should().Throw<InvalidOperationException>().WithMessage("Only approved lots can be published");
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
    public void Complete_SetsStatus()
    {
        var lot = CreateActiveLot();
        lot.Complete();
        lot.Status.Should().Be(LotStatus.Completed);
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

    // --- Complete ---

    [Fact]
    public void Complete_SetsStatusAndWinner()
    {
        var lot = CreateActiveLot();
        lot.Complete();
        lot.Status.Should().Be(LotStatus.Completed);
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
        lot.PlaceBid(Money.FromDecimal(1500m), BidderId, "Bidder");
        lot.ClearDomainEvents();
        lot.Complete("WinnerName");

        lot.DomainEvents.Should().ContainSingle(e => e is AuctionCompletedDomainEvent);
        var evt = lot.DomainEvents.OfType<AuctionCompletedDomainEvent>().Single();
        evt.LotId.Should().Be(lot.Id);
        evt.FinalPrice.Should().Be(1500m);
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
        lot.Complete();
        lot.OpenDispute("Item not as described");
        lot.Status.Should().Be(LotStatus.Disputed);
        lot.DisputeReason.Should().Be("Item not as described");
    }

    [Fact]
    public void ResolveDispute_InFavorOfBuyer_Cancels()
    {
        var lot = CreateActiveLot();
        lot.Complete();
        lot.OpenDispute("broken");
        lot.ResolveDispute(true);
        lot.Status.Should().Be(LotStatus.Cancelled);
    }

    [Fact]
    public void ResolveDispute_InFavorOfSeller_Completes()
    {
        var lot = CreateActiveLot();
        lot.Complete();
        lot.OpenDispute("broken");
        lot.ResolveDispute(false);
        lot.Status.Should().Be(LotStatus.TransactionComplete);
    }
}
