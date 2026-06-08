using AuHub.Shared.ValueObjects;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Payment.Application.Commands.ReleaseFunds;
using Payment.Application.Repositories;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.UnitTests;

public class ReleaseFundsCommandHandlerTests
{
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ReleaseFundsCommandHandler _handler;

    public ReleaseFundsCommandHandlerTests()
    {
        _walletRepo = Substitute.For<IWalletRepository>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _transactionRepo.GetByUserIdTypeAndReferenceIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<TransactionType>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);
        _handler = new ReleaseFundsCommandHandler(_walletRepo, _transactionRepo);
    }

    private static ReleaseFundsCommand CreateCommand()
    {
        return new ReleaseFundsCommand
        {
            UserId = Guid.NewGuid(),
            Amount = Money.FromDecimal(200),
            ReferenceId = Guid.NewGuid()
        };
    }

    [Fact]
    public async Task HandleAsync_WithFrozenFunds_UnfreezesAndWritesTransaction()
    {
        var command = CreateCommand();
        var wallet = Wallet.Create(command.UserId);
        wallet.Deposit(Money.FromDecimal(500));
        wallet.Freeze(command.Amount);
        _walletRepo.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        wallet.Balance.Should().Be(Money.FromDecimal(500));
        wallet.FrozenBalance.Should().Be(Money.Zero);
        await _transactionRepo.Received(1).AddAsync(Arg.Is<Transaction>(t =>
            t.UserId == command.UserId &&
            t.Type == TransactionType.Release &&
            t.Amount == command.Amount &&
            t.ReferenceId == command.ReferenceId
        ), Arg.Any<CancellationToken>());
        await _walletRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateRelease_ReturnsSuccessWithoutUnfreezingAgain()
    {
        var command = CreateCommand();
        var existing = Transaction.Create(
            command.UserId,
            TransactionType.Release,
            command.Amount,
            "Existing release",
            command.ReferenceId);
        _transactionRepo.GetByUserIdTypeAndReferenceIdAsync(
                command.UserId,
                TransactionType.Release,
                command.ReferenceId,
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await _walletRepo.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _transactionRepo.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WalletNotFound_ReturnsFailure()
    {
        var command = CreateCommand();
        _walletRepo.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        await _transactionRepo.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InsufficientFrozenFunds_ReturnsFailure()
    {
        var command = CreateCommand();
        var wallet = Wallet.Create(command.UserId);
        wallet.Deposit(Money.FromDecimal(500));
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
        result.Error.Should().Contain("Failed to release funds");
        result.StatusCode.Should().Be(500);
    }
}
