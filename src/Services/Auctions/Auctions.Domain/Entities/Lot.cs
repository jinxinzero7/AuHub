using Auctions.Domain.Events;
using Auctions.Domain.Enums;
using AuHub.Shared.ValueObjects;

namespace Auctions.Domain.Entities;

public class Lot
{
    public static readonly TimeSpan SniperProtectionWindow = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan SniperProtectionExtension = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan MaxSniperProtectionExtension = TimeSpan.FromMinutes(10);

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money StartingPrice { get; private set; } = Money.Zero;
    public Money CurrentPrice { get; private set; } = Money.Zero;

    public TimeSpan Duration { get; private set; }
    public DateTime? StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }

    public Guid SellerId { get; private set; }
    public LotStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? WinnerId { get; private set; }

    public string? AdminComment { get; private set; }

    public string? TrackingNumber { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public string? DeliveryRecipientName { get; private set; }
    public string? DeliveryRecipientPhone { get; private set; }
    public DeliveryProvider? SelectedDeliveryProvider { get; private set; }
    public DateTime? DeliveryRequestedAt { get; private set; }
    public DateTime? DeliveryRequestDeadlineAt { get; private set; }
    public string? DisputeReason { get; private set; }
    public List<DeliveryProvider> SupportedDeliveryProviders { get; private set; } = new();

    public uint RowVersion { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    private readonly List<Bid> _bids = new();
    public IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();

    private readonly List<LotImage> _images = new();
    public IReadOnlyCollection<LotImage> Images => _images.AsReadOnly();

    public string? CoverImageUrl => _images.FirstOrDefault()?.Id.ToString();
    public int ImagesCount => _images.Count;
    public bool IsEditable => !IsDeleted && Status is LotStatus.Draft or LotStatus.Rejected;

    private Lot() { }

    public static Lot Create(
        string title,
        string description,
        Money startingPrice,
        TimeSpan duration,
        Guid sellerId,
        IEnumerable<DeliveryProvider> supportedDeliveryProviders)
    {
        var providers = NormalizeDeliveryProviders(supportedDeliveryProviders);

        return new Lot
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartingPrice = startingPrice,
            CurrentPrice = startingPrice,
            Duration = duration,
            SellerId = sellerId,
            SupportedDeliveryProviders = providers,
            Status = LotStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SubmitForModeration()
    {
        if (Status != LotStatus.Draft)
            throw new InvalidOperationException("Only draft lots can be submitted for moderation");

        Status = LotStatus.PendingModeration;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        SubmitForModeration();
    }

    public void Approve()
    {
        if (Status != LotStatus.PendingModeration)
            throw new InvalidOperationException("Only lots pending moderation can be approved");

        StartTime = DateTime.UtcNow;
        EndTime = StartTime.Value.Add(Duration);
        Status = LotStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        if (Status != LotStatus.PendingModeration)
            throw new InvalidOperationException("Only lots pending moderation can be rejected");

        Status = LotStatus.Rejected;
        AdminComment = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDraft(
        string title,
        string description,
        Money startingPrice,
        TimeSpan duration,
        IEnumerable<DeliveryProvider> supportedDeliveryProviders)
    {
        if (!IsEditable)
            throw new InvalidOperationException("Only draft or rejected lots can be edited");

        var providers = NormalizeDeliveryProviders(supportedDeliveryProviders);

        Title = title;
        Description = description;
        StartingPrice = startingPrice;
        CurrentPrice = startingPrice;
        Duration = duration;
        SupportedDeliveryProviders = providers;
        Status = LotStatus.Draft;
        AdminComment = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PlaceBid(Money amount, Guid bidderId, string bidderName)
    {
        if (Status != LotStatus.Active)
            throw new InvalidOperationException("Lot is not active");

        if (bidderId == SellerId)
            throw new InvalidOperationException("Seller cannot bid on own lot");

        if (amount <= CurrentPrice)
            throw new InvalidOperationException("Bid amount must be higher than current price");

        if (!EndTime.HasValue || DateTime.UtcNow > EndTime.Value)
            throw new InvalidOperationException("Auction has ended");

        CurrentPrice = amount;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new BidPlacedDomainEvent
        {
            LotId = Id,
            BidderId = bidderId,
            BidderName = bidderName,
            Amount = amount.Amount,
            SellerId = SellerId,
            LotTitle = Title
        });
    }

    public void ExtendEndTime(TimeSpan extension)
    {
        if (!EndTime.HasValue)
            return;

        EndTime = EndTime.Value.Add(extension);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool ApplySniperProtection(DateTime now)
    {
        if (!StartTime.HasValue || !EndTime.HasValue)
            return false;

        if (now > EndTime.Value || EndTime.Value - now >= SniperProtectionWindow)
            return false;

        var maxEndTime = StartTime.Value.Add(Duration).Add(MaxSniperProtectionExtension);
        if (EndTime.Value >= maxEndTime)
            return false;

        var nextEndTime = EndTime.Value.Add(SniperProtectionExtension);
        EndTime = nextEndTime > maxEndTime ? maxEndTime : nextEndTime;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public void Complete(string? winnerName = null)
    {
        if (Status != LotStatus.Active)
            throw new InvalidOperationException("Only active lots can be completed");

        WinnerId = _bids.OrderByDescending(b => b.Amount).Select(b => (Guid?)b.BidderId).FirstOrDefault();
        Status = WinnerId.HasValue ? LotStatus.Completed : LotStatus.CompletedNoWinner;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new AuctionCompletedDomainEvent
        {
            LotId = Id,
            LotTitle = Title,
            WinnerId = WinnerId,
            WinnerName = winnerName,
            FinalPrice = CurrentPrice.Amount,
            SellerId = SellerId
        });
    }

    public void Cancel()
    {
        if (Status == LotStatus.Cancelled)
            throw new InvalidOperationException("Lot is already cancelled");

        if (Status == LotStatus.Completed ||
            Status == LotStatus.CompletedNoWinner ||
            Status == LotStatus.Delivered ||
            Status == LotStatus.TransactionComplete)
            throw new InvalidOperationException("Cannot cancel finalized lot");

        if (Status == LotStatus.Active && _bids.Count > 0)
            throw new InvalidOperationException("Cannot cancel active lot with bids");

        Status = LotStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Freeze()
    {
        if (Status != LotStatus.Active)
            throw new InvalidOperationException("Only active lots can be frozen");

        Status = LotStatus.Frozen;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unfreeze()
    {
        if (Status != LotStatus.Frozen)
            throw new InvalidOperationException("Only frozen lots can be unfrozen");

        Status = LotStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RequestDelivery(
        DeliveryProvider provider,
        string address,
        string recipientName,
        string recipientPhone)
    {
        if (Status != LotStatus.DeliveryRequestPending)
            throw new InvalidOperationException("Cannot set delivery address in current status");

        if (!SupportedDeliveryProviders.Contains(provider))
            throw new InvalidOperationException("Delivery provider is not supported for this lot");

        if (DeliveryRequestDeadlineAt.HasValue && DateTime.UtcNow > DeliveryRequestDeadlineAt.Value)
            throw new InvalidOperationException("Delivery request deadline has expired");

        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidOperationException("Delivery address is required");

        if (string.IsNullOrWhiteSpace(recipientName))
            throw new InvalidOperationException("Recipient name is required");

        if (string.IsNullOrWhiteSpace(recipientPhone))
            throw new InvalidOperationException("Recipient phone is required");

        SelectedDeliveryProvider = provider;
        DeliveryAddress = address;
        DeliveryRecipientName = recipientName;
        DeliveryRecipientPhone = recipientPhone;
        DeliveryRequestedAt = DateTime.UtcNow;
        Status = LotStatus.ShippingPending;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Ship(string trackingNumber)
    {
        if (Status != LotStatus.ShippingPending)
            throw new InvalidOperationException("Lot is not in shipping pending status");

        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new InvalidOperationException("Tracking number is required");

        TrackingNumber = trackingNumber;
        Status = LotStatus.Shipped;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkShipped(string trackingNumber)
    {
        Ship(trackingNumber);
    }

    public void ConfirmDelivery()
    {
        if (Status != LotStatus.Shipped)
            throw new InvalidOperationException("Lot is not shipped");

        Status = LotStatus.Delivered;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CompleteTransaction()
    {
        if (Status != LotStatus.Delivered)
            throw new InvalidOperationException("Only delivered lots can be completed");

        Status = LotStatus.TransactionComplete;
        UpdatedAt = DateTime.UtcNow;
    }

    public void OpenDispute(string reason)
    {
        if (Status != LotStatus.Completed &&
            Status != LotStatus.DeliveryRequestPending &&
            Status != LotStatus.ShippingPending &&
            Status != LotStatus.Shipped &&
            Status != LotStatus.Delivered)
            throw new InvalidOperationException("Cannot dispute lot in current status");

        Status = LotStatus.Disputed;
        DisputeReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void OpenDeliveryRequestWindow()
    {
        if (Status != LotStatus.Completed)
            throw new InvalidOperationException("Only completed lots can open delivery request window");

        if (!WinnerId.HasValue)
            throw new InvalidOperationException("Only completed lots with a winner can open delivery request window");

        Status = LotStatus.DeliveryRequestPending;
        DeliveryRequestDeadlineAt = DateTime.UtcNow.AddDays(3);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ExpireDeliveryRequest()
    {
        if (Status != LotStatus.DeliveryRequestPending)
            throw new InvalidOperationException("Only lots pending delivery request can expire");

        Status = LotStatus.DeliveryRequestExpired;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResolveDispute(bool inFavorOfBuyer)
    {
        if (Status != LotStatus.Disputed)
            throw new InvalidOperationException("Lot is not in dispute");

        Status = inFavorOfBuyer ? LotStatus.Cancelled : LotStatus.TransactionComplete;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddImage(LotImage image)
    {
        _images.Add(image);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image == null) return false;

        _images.Remove(image);
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    private static List<DeliveryProvider> NormalizeDeliveryProviders(IEnumerable<DeliveryProvider> supportedDeliveryProviders)
    {
        var providers = supportedDeliveryProviders
            .Distinct()
            .OrderBy(provider => provider)
            .ToList();

        if (providers.Count == 0)
            throw new InvalidOperationException("At least one delivery provider is required");

        return providers;
    }

    public void SoftDelete(Guid? deletedBy = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Lot is already deleted");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
