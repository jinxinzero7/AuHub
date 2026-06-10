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
        _transactionRepo.GetByUserIdTypeAndReferenceIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<TransactionType>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);
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
    public async Task HandleAsync_DuplicateTransfer_ReturnsSuccessWithoutDepositingAgain()
    {
        var command = CreateCommand();
        var existing = Transaction.Create(
            command.SellerId,
            TransactionType.Transfer,
            command.Amount,
            "Existing transfer",
            command.ReferenceId);
        _transactionRepo.GetByUserIdTypeAndReferenceIdAsync(
                command.SellerId,
                TransactionType.Transfer,
                command.ReferenceId,
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await _walletRepo.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _walletRepo.DidNotReceive().AddAsync(Arg.Any<Wallet>(), Arg.Any<CancellationToken>());
        await _transactionRepo.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
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
    public async Task HandleAsync_WithServiceFee_DepositsFeeToPlatformWalletAndWritesTransaction()
    {
        var command = CreateCommand() with { ServiceFee = Money.FromDecimal(10m) };
        var sellerWallet = Wallet.Create(command.SellerId);
        var platformWallet = Wallet.Create(TransferToSellerCommand.PlatformWalletUserId);
        _walletRepo.GetByUserIdAsync(command.SellerId, Arg.Any<CancellationToken>()).Returns(sellerWallet);
        _walletRepo.GetByUserIdAsync(TransferToSellerCommand.PlatformWalletUserId, Arg.Any<CancellationToken>()).Returns(platformWallet);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        sellerWallet.Balance.Should().Be(command.Amount);
        platformWallet.Balance.Should().Be(command.ServiceFee);
        await _transactionRepo.Received(1).AddAsync(Arg.Is<Transaction>(t =>
            t.UserId == TransferToSellerCommand.PlatformWalletUserId &&
            t.Type == TransactionType.ServiceFee &&
            t.Amount == command.ServiceFee &&
            t.ReferenceId == command.ReferenceId
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
