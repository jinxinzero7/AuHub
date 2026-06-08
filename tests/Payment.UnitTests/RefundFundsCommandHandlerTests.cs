using AuHub.Shared.ValueObjects;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Payment.Application.Commands.RefundFunds;
using Payment.Application.Repositories;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.UnitTests;

public class RefundFundsCommandHandlerTests
{
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly RefundFundsCommandHandler _handler;

    public RefundFundsCommandHandlerTests()
    {
        _walletRepo = Substitute.For<IWalletRepository>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _transactionRepo.GetByUserIdTypeAndReferenceIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<TransactionType>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);
        _handler = new RefundFundsCommandHandler(_walletRepo, _transactionRepo);
    }

    private static RefundFundsCommand CreateCommand()
    {
        return new RefundFundsCommand
        {
            UserId = Guid.NewGuid(),
            Amount = Money.FromDecimal(200),
            ReferenceId = Guid.NewGuid(),
            Reason = "Auction cancelled"
        };
    }

    [Fact]
    public async Task HandleAsync_ExistingWallet_DepositsRefundAndWritesTransaction()
    {
        var command = CreateCommand();
        var wallet = Wallet.Create(command.UserId);
        wallet.Deposit(Money.FromDecimal(300));
        _walletRepo.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        wallet.Balance.Should().Be(Money.FromDecimal(500));
        await _walletRepo.DidNotReceive().AddAsync(Arg.Any<Wallet>(), Arg.Any<CancellationToken>());
        await _transactionRepo.Received(1).AddAsync(Arg.Is<Transaction>(t =>
            t.UserId == command.UserId &&
            t.Type == TransactionType.Refund &&
            t.Amount == command.Amount &&
            t.ReferenceId == command.ReferenceId
        ), Arg.Any<CancellationToken>());
        await _walletRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateRefund_ReturnsSuccessWithoutDepositingAgain()
    {
        var command = CreateCommand();
        var existing = Transaction.Create(
            command.UserId,
            TransactionType.Refund,
            command.Amount,
            "Existing refund",
            command.ReferenceId);
        _transactionRepo.GetByUserIdTypeAndReferenceIdAsync(
                command.UserId,
                TransactionType.Refund,
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
    public async Task HandleAsync_WalletNotFound_CreatesWalletAndDepositsRefund()
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
    }

    [Fact]
    public async Task HandleAsync_WithZeroAmount_ReturnsValidationFailure()
    {
        var command = CreateCommand() with { Amount = Money.Zero };
        var wallet = Wallet.Create(command.UserId);
        _walletRepo.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(wallet);

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
        result.Error.Should().Contain("Failed to refund");
        result.StatusCode.Should().Be(500);
    }
}
