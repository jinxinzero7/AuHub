using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using Payment.Application.Commands.ReserveFunds;
using Payment.Application.Repositories;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Payment.UnitTests;

public class ReserveFundsCommandHandlerTests
{
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ReserveFundsCommandHandler _handler;

    public ReserveFundsCommandHandlerTests()
    {
        _walletRepo = Substitute.For<IWalletRepository>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _transactionRepo.GetByUserIdTypeAndReferenceIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<TransactionType>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);
        _handler = new ReserveFundsCommandHandler(_walletRepo, _transactionRepo);
    }

    private ReserveFundsCommand CreateCommand()
    {
        return new ReserveFundsCommand
        {
            UserId = Guid.NewGuid(),
            Amount = Money.FromDecimal(200),
            ReferenceId = Guid.NewGuid()
        };
    }

    [Fact]
    public async Task HandleAsync_WithSufficientBalance_ReservesFunds()
    {
        var command = CreateCommand();
        var wallet = Wallet.Create(command.UserId);
        wallet.Deposit(Money.FromDecimal(500));
        _walletRepo.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        wallet.Balance.Should().Be(Money.FromDecimal(300));
        wallet.FrozenBalance.Should().Be(Money.FromDecimal(200));
        await _transactionRepo.Received(1).AddAsync(Arg.Is<Transaction>(t =>
            t.UserId == command.UserId &&
            t.Type == TransactionType.Reserve &&
            t.Amount == command.Amount &&
            t.ReferenceId == command.ReferenceId
        ), Arg.Any<CancellationToken>());
        await _walletRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateReserve_ReturnsSuccessWithoutFreezingAgain()
    {
        var command = CreateCommand();
        var existing = Transaction.Create(
            command.UserId,
            TransactionType.Reserve,
            command.Amount,
            "Existing reserve",
            command.ReferenceId);
        _transactionRepo.GetByUserIdTypeAndReferenceIdAsync(
                command.UserId,
                TransactionType.Reserve,
                command.ReferenceId,
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await _walletRepo.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _transactionRepo.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateReserveWithDifferentAmount_ReturnsConflict()
    {
        var command = CreateCommand();
        var existing = Transaction.Create(
            command.UserId,
            TransactionType.Reserve,
            Money.FromDecimal(250),
            "Existing reserve",
            command.ReferenceId);
        _transactionRepo.GetByUserIdTypeAndReferenceIdAsync(
                command.UserId,
                TransactionType.Reserve,
                command.ReferenceId,
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(409);
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
        result.Error.Should().Be("Wallet not found. Please top up your balance first.");
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task HandleAsync_InsufficientBalance_ReturnsFailure()
    {
        var command = CreateCommand();
        var wallet = Wallet.Create(command.UserId);
        wallet.Deposit(Money.FromDecimal(50));
        _walletRepo.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryThrows_ReturnsFailure()
    {
        _walletRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DB error"));

        var result = await _handler.HandleAsync(CreateCommand());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to reserve funds");
        result.StatusCode.Should().Be(500);
    }
}
