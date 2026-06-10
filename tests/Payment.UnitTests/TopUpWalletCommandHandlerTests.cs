using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using Payment.Application.Commands.TopUpWallet;
using Payment.Application.Repositories;
using Payment.Application.Services;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Payment.UnitTests;

public class TopUpWalletCommandHandlerTests
{
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IPaymentProvider _paymentProvider;
    private readonly TopUpWalletCommandHandler _handler;

    public TopUpWalletCommandHandlerTests()
    {
        _walletRepo = Substitute.For<IWalletRepository>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _paymentProvider = Substitute.For<IPaymentProvider>();
        _paymentProvider.ConfirmTopUpAsync(Arg.Any<Guid>(), Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PaymentProviderConfirmation("TestProvider", Guid.NewGuid().ToString("N"))));
        _handler = new TopUpWalletCommandHandler(_walletRepo, _transactionRepo, _paymentProvider);
    }

    private TopUpWalletCommand CreateCommand()
    {
        return new TopUpWalletCommand
        {
            UserId = Guid.NewGuid(),
            Amount = Money.FromDecimal(500)
        };
    }

    [Fact]
    public async Task HandleAsync_NewWallet_CreatesAndDeposits()
    {
        var command = CreateCommand();
        _walletRepo.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await _walletRepo.Received(1).AddAsync(Arg.Is<Wallet>(w =>
            w.UserId == command.UserId &&
            w.Balance == command.Amount &&
            w.FrozenBalance == Money.Zero
        ), Arg.Any<CancellationToken>());
        await _transactionRepo.Received(1).AddAsync(Arg.Is<Transaction>(t =>
            t.UserId == command.UserId &&
            t.Type == TransactionType.Deposit &&
            t.Amount == command.Amount
        ), Arg.Any<CancellationToken>());
        await _walletRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ExistingWallet_Deposits()
    {
        var command = CreateCommand();
        var wallet = Wallet.Create(command.UserId);
        wallet.Deposit(Money.FromDecimal(300));
        _walletRepo.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        wallet.Balance.Should().Be(Money.FromDecimal(800));
        await _walletRepo.DidNotReceive().AddAsync(Arg.Any<Wallet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithZeroAmount_ReturnsFailure()
    {
        var command = CreateCommand();
        command = command with { Amount = Money.Zero };
        _paymentProvider.ConfirmTopUpAsync(command.UserId, command.Amount, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PaymentProviderConfirmation>("Amount must be positive", 400));

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Amount must be positive");
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_WhenProviderRejectsPayment_DoesNotMutateWallet()
    {
        var command = CreateCommand();
        _paymentProvider.ConfirmTopUpAsync(command.UserId, command.Amount, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PaymentProviderConfirmation>("Payment rejected", 402));

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Payment rejected");
        result.StatusCode.Should().Be(402);
        await _walletRepo.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _transactionRepo.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _walletRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB error"));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to top up wallet");
        result.StatusCode.Should().Be(500);
    }
}
