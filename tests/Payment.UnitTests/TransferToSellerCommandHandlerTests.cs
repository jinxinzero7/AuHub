using AuHub.Shared.ValueObjects;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Payment.Application.Commands.TransferToSeller;
using Payment.Application.Repositories;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.UnitTests;

public class TransferToSellerCommandHandlerTests
{
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly TransferToSellerCommandHandler _handler;

    public TransferToSellerCommandHandlerTests()
    {
        _walletRepo = Substitute.For<IWalletRepository>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _handler = new TransferToSellerCommandHandler(_walletRepo, _transactionRepo);
    }

    private static TransferToSellerCommand CreateCommand()
    {
        return new TransferToSellerCommand
        {
            SellerId = Guid.NewGuid(),
            Amount = Money.FromDecimal(990),
            ReferenceId = Guid.NewGuid()
        };
    }

    [Fact]
    public async Task HandleAsync_ExistingWallet_DepositsPayoutAndWritesTransaction()
    {
        var command = CreateCommand();
        var wallet = Wallet.Create(command.SellerId);
        wallet.Deposit(Money.FromDecimal(10));
        _walletRepo.GetByUserIdAsync(command.SellerId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        wallet.Balance.Should().Be(Money.FromDecimal(1000));
        await _walletRepo.DidNotReceive().AddAsync(Arg.Any<Wallet>(), Arg.Any<CancellationToken>());
        await _transactionRepo.Received(1).AddAsync(Arg.Is<Transaction>(t =>
            t.UserId == command.SellerId &&
            t.Type == TransactionType.Transfer &&
            t.Amount == command.Amount &&
            t.ReferenceId == command.ReferenceId
        ), Arg.Any<CancellationToken>());
        await _walletRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WalletNotFound_CreatesWalletAndDepositsPayout()
    {
        var command = CreateCommand();
        _walletRepo.GetByUserIdAsync(command.SellerId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await _walletRepo.Received(1).AddAsync(Arg.Is<Wallet>(w =>
            w.UserId == command.SellerId &&
            w.Balance == command.Amount &&
            w.FrozenBalance == Money.Zero
        ), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithZeroAmount_ReturnsValidationFailure()
    {
        var command = CreateCommand() with { Amount = Money.Zero };
        var wallet = Wallet.Create(command.SellerId);
        _walletRepo.GetByUserIdAsync(command.SellerId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        await _transactionRepo.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _walletRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB error"));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to transfer to seller");
        result.StatusCode.Should().Be(500);
    }
}
