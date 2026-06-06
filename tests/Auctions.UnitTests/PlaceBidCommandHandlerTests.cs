using AuHub.Shared.Contracts;
using AuHub.Shared.ValueObjects;
using Auctions.Application.Commands.PlaceBid;
using Auctions.Application.Services;
using Auctions.Domain.Entities;
using Auctions.Domain.Events;
using Auctions.Domain.Interfaces;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Auctions.UnitTests;

public class PlaceBidCommandHandlerTests
{
    private readonly ILotRepository _lotRepo;
    private readonly IBidRepository _bidRepo;
    private readonly IEventPublisher _eventPublisher;
    private readonly IOutbox _outbox;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IPaymentClient _paymentClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly PlaceBidCommandHandler _handler;

    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid BidderId = Guid.NewGuid();
    private static readonly Guid LotId = Guid.NewGuid();

    public PlaceBidCommandHandlerTests()
    {
        _lotRepo = Substitute.For<ILotRepository>();
        _bidRepo = Substitute.For<IBidRepository>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _outbox = Substitute.For<IOutbox>();
        _domainEventDispatcher = Substitute.For<IDomainEventDispatcher>();
        _paymentClient = Substitute.For<IPaymentClient>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();

        _handler = new PlaceBidCommandHandler(
            _lotRepo, _bidRepo, _eventPublisher, _outbox,
            _domainEventDispatcher, _paymentClient, _publishEndpoint);
    }

    private Lot CreateActiveLot()
    {
        var lot = Lot.Create("Test Lot", "Desc", Money.FromDecimal(1000m), TimeSpan.FromDays(3), SellerId);
        lot.SubmitForModeration();
        lot.Approve();
        return lot;
    }

    private PlaceBidCommand CreateCommand(decimal amount = 1500m, Guid? bidderId = null)
    {
        return new PlaceBidCommand
        {
            LotId = LotId,
            BidderId = bidderId ?? BidderId,
            BidderName = "TestBidder",
            Amount = Money.FromDecimal(amount),
            IdempotencyKey = null
        };
    }

    [Fact]
    public async Task HandleAsync_ValidBid_ReturnsSuccess()
    {
        var lot = CreateActiveLot();
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(true, 5000m));
        _paymentClient.ReserveFundsAsync(BidderId, 1500m, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.NewCurrentPrice.Should().Be(Money.FromDecimal(1500m));
    }

    [Fact]
    public async Task HandleAsync_ValidBid_SavesToRepositories()
    {
        var lot = CreateActiveLot();
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(true, 5000m));
        _paymentClient.ReserveFundsAsync(BidderId, 1500m, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        await _handler.HandleAsync(CreateCommand());

        await _bidRepo.Received(1).AddAsync(Arg.Any<Bid>(), Arg.Any<CancellationToken>());
        await _bidRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _lotRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_IdempotencyKeyMatch_ReturnsEarly()
    {
        var existingBid = Bid.Create(LotId, BidderId, Money.FromDecimal(1500m), Guid.NewGuid());
        _bidRepo.GetByIdempotencyKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(existingBid);

        var command = new PlaceBidCommand
        {
            LotId = LotId, BidderId = BidderId, BidderName = "Test",
            Amount = Money.FromDecimal(1500m), IdempotencyKey = Guid.NewGuid()
        };

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Message.Should().Be("Bid already exists (idempotent retry)");
        await _lotRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_LotNotFound_ReturnsFailure()
    {
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns((Lot?)null);

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Lot not found");
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task HandleAsync_SelfBid_ReturnsFailure()
    {
        var lot = CreateActiveLot();
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);

        var result = await _handler.HandleAsync(CreateCommand(bidderId: SellerId));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("You cannot bid on your own lot");
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task HandleAsync_PaymentServiceDown_ReturnsFailure()
    {
        var lot = CreateActiveLot();
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(false, 0, "Unavailable"));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Payment service unavailable");
        result.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task HandleAsync_InsufficientFunds_ReturnsFailure()
    {
        var lot = CreateActiveLot();
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(true, 500m));

        var result = await _handler.HandleAsync(CreateCommand(1500m));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Insufficient funds");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_DoesNotReleaseSameBidderFunds()
    {
        var lot = CreateActiveLot();
        lot.PlaceBid(Money.FromDecimal(1200m), BidderId, "SameBidder");
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(true, 5000m));
        _paymentClient.ReserveFundsAsync(BidderId, 1500m, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        await _handler.HandleAsync(CreateCommand(1500m));

        await _paymentClient.DidNotReceive().ReleaseFundsAsync(
            Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SniperProtection_ExtendsEndTime()
    {
        var lot = CreateActiveLot();
        lot.ExtendEndTime(-lot.Duration + TimeSpan.FromSeconds(25));

        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(true, 5000m));
        _paymentClient.ReserveFundsAsync(BidderId, 1500m, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        await _handler.HandleAsync(CreateCommand(1500m));

        lot.EndTime.Should().BeAfter(DateTime.UtcNow.Add(TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task HandleAsync_InvokesEventPublisherAndMassTransit()
    {
        var lot = CreateActiveLot();
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(true, 5000m));
        _paymentClient.ReserveFundsAsync(BidderId, 1500m, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        await _handler.HandleAsync(CreateCommand());

        await _eventPublisher.Received(1).PublishNewBidAsync(
            Arg.Any<Guid>(), 1500m, "TestBidder", Arg.Any<CancellationToken>());
        await _publishEndpoint.Received(1).Publish(
            Arg.Any<BidPlacedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DispatchesDomainEvents()
    {
        var lot = CreateActiveLot();
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(true, 5000m));
        _paymentClient.ReserveFundsAsync(BidderId, 1500m, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        await _handler.HandleAsync(CreateCommand());

        await _domainEventDispatcher.Received(1).DispatchAllAsync(
            Arg.Any<IEnumerable<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WritesToOutbox()
    {
        var lot = CreateActiveLot();
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(true, 5000m));
        _paymentClient.ReserveFundsAsync(BidderId, 1500m, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        await _handler.HandleAsync(CreateCommand());

        await _outbox.Received(1).AddAsync("BidPlaced", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidOperationException_ReturnsFailure()
    {
        var lot = CreateActiveLot();
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(lot);
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(true, 5000m));
        _paymentClient.ReserveFundsAsync(BidderId, 1500m, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Domain error"));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Domain error");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_ConcurrencyException_Retries()
    {
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(_ => CreateActiveLot());
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(true, 5000m));
        _paymentClient.ReserveFundsAsync(BidderId, 1500m, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        bool firstCall = true;
        _bidRepo.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                if (firstCall)
                {
                    firstCall = false;
                    throw new DbUpdateConcurrencyException("Concurrency");
                }
            });

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ConcurrencyExhausted_ReturnsError()
    {
        var lot = CreateActiveLot();
        _lotRepo.GetByIdAsync(LotId, Arg.Any<CancellationToken>()).Returns(_ => CreateActiveLot());
        _paymentClient.GetBalanceAsync(BidderId, Arg.Any<CancellationToken>())
            .Returns(new BalanceResult(true, 5000m));
        _paymentClient.ReserveFundsAsync(BidderId, 1500m, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentResult(true));

        _bidRepo.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => throw new DbUpdateConcurrencyException("Concurrency"));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
    }
}
