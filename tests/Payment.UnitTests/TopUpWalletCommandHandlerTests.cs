using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using Payment.Application.Commands.TopUpWallet;
using Payment.Application.Repositories;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Payment.UnitTests;

public class TopUpWalletCommandHandlerTests
{
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly TopUpWalletCommandHandler _handler;

    public TopUpWalletCommandHandlerTests()
    {
        _walletRepo = Substitute.For<IWalletRepository>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _handler = new TopUpWalletCommandHandler(_walletRepo, _transactionRepo);
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
        _walletRepo.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("positive");
        result.StatusCode.Should().Be(400);
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
