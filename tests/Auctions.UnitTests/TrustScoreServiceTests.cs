using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Enums;
using Auctions.Domain.Interfaces;
using AuHub.Shared.ValueObjects;
using FluentAssertions;

namespace Auctions.UnitTests;

public class TrustScoreServiceTests
{
    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid BuyerId = Guid.NewGuid();

    [Fact]
    public async Task RecordSuccessfulSaleAsync_AddsSellerEventOnce()
    {
        var repository = new InMemoryTrustScoreEventRepository();
        var service = new TrustScoreService(repository);
        var lot = CreateLot();

        await service.RecordSuccessfulSaleAsync(lot);
        await service.RecordSuccessfulSaleAsync(lot);

        repository.Events.Should().ContainSingle(trustEvent =>
            trustEvent.UserId == SellerId &&
            trustEvent.Subject == TrustScoreSubject.Seller &&
            trustEvent.Reason == TrustScoreReason.SuccessfulSale &&
            trustEvent.Points == 5 &&
            trustEvent.ReferenceId == lot.Id);
    }

    [Fact]
    public async Task RecordDisputeResolvedAsync_InFavorOfBuyer_PenalizesSeller()
    {
        var repository = new InMemoryTrustScoreEventRepository();
        var service = new TrustScoreService(repository);
        var lot = CreateLot();

        await service.RecordDisputeResolvedAsync(lot, inFavorOfBuyer: true);

        repository.Events.Should().ContainSingle(trustEvent =>
            trustEvent.UserId == SellerId &&
            trustEvent.Subject == TrustScoreSubject.Seller &&
            trustEvent.Reason == TrustScoreReason.SellerLostDispute &&
            trustEvent.Points == -15);
    }

    [Fact]
    public async Task RecordDisputeResolvedAsync_InFavorOfSeller_PenalizesBuyer()
    {
        var repository = new InMemoryTrustScoreEventRepository();
        var service = new TrustScoreService(repository);
        var lot = CreateLot();
        typeof(Lot).GetProperty(nameof(Lot.WinnerId))!.SetValue(lot, BuyerId);

        await service.RecordDisputeResolvedAsync(lot, inFavorOfBuyer: false);

        repository.Events.Should().ContainSingle(trustEvent =>
            trustEvent.UserId == BuyerId &&
            trustEvent.Subject == TrustScoreSubject.Buyer &&
            trustEvent.Reason == TrustScoreReason.BuyerLostDispute &&
            trustEvent.Points == -8);
    }

    [Fact]
    public async Task GetSellerTrustScoreAsync_ReturnsPublicScoreAndBadge()
    {
        var repository = new InMemoryTrustScoreEventRepository();
        var service = new TrustScoreService(repository);
        var lot1 = CreateLot();
        var lot2 = CreateLot();

        await service.RecordSuccessfulSaleAsync(lot1);
        await service.RecordSuccessfulSaleAsync(lot2);

        var score = await service.GetSellerTrustScoreAsync(SellerId);

        score.Score.Should().Be(80);
        score.Badge.Should().Be("Reliable");
        score.EventsCount.Should().Be(2);
        score.SuccessfulSales.Should().Be(2);
        score.SellerLostDisputes.Should().Be(0);
    }

    private static Lot CreateLot()
    {
        return Lot.Create(
            "Lot",
            "Description",
            Money.FromDecimal(1000m),
            TimeSpan.FromDays(3),
            SellerId,
            [DeliveryProvider.Cdek]);
    }

    private sealed class InMemoryTrustScoreEventRepository : ITrustScoreEventRepository
    {
        public List<TrustScoreEvent> Events { get; } = new();

        public Task<bool> ExistsAsync(
            Guid userId,
            TrustScoreSubject subject,
            TrustScoreReason reason,
            Guid referenceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Events.Any(trustEvent =>
                trustEvent.UserId == userId &&
                trustEvent.Subject == subject &&
                trustEvent.Reason == reason &&
                trustEvent.ReferenceId == referenceId));
        }

        public Task<List<TrustScoreEvent>> GetByUserIdAsync(
            Guid userId,
            TrustScoreSubject subject,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Events
                .Where(trustEvent => trustEvent.UserId == userId && trustEvent.Subject == subject)
                .ToList());
        }

        public Task AddAsync(TrustScoreEvent trustScoreEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(trustScoreEvent);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
