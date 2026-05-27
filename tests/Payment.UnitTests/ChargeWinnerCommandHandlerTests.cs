using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using FluentAssertions;
using Payment.Application.Commands.ChargeWinner;
using Payment.Application.Repositories;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Payment.UnitTests;

public class ChargeWinnerCommandHandlerTests
{
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ChargeWinnerCommandHandler _handler;

    public ChargeWinnerCommandHandlerTests()
    {
        _walletRepo = Substitute.For<IWalletRepository>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _handler = new ChargeWinnerCommandHandler(_walletRepo, _transactionRepo);
    }

    private ChargeWinnerCommand CreateCommand()
    {
        return new ChargeWinnerCommand
        {
            UserId = Guid.NewGuid(),
            Amount = Money.FromDecimal(200),
            ReferenceId = Guid.NewGuid()
        };
    }

    [Fact]
    public async Task HandleAsync_WithSufficientFrozen_ChargesWinner()
    {
        var command = CreateCommand();
        var wallet = Wallet.Create(command.UserId);
        wallet.Deposit(Money.FromDecimal(500));
        wallet.Freeze(command.Amount);
        _walletRepo.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(wallet);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        wallet.FrozenBalance.Should().Be(Money.Zero);
        wallet.Balance.Should().Be(Money.FromDecimal(300));
        await _transactionRepo.Received(1).AddAsync(Arg.Is<Transaction>(t =>
            t.UserId == command.UserId &&
            t.Type == TransactionType.Win &&
            t.Amount == command.Amount &&
            t.ReferenceId == command.ReferenceId
        ), Arg.Any<CancellationToken>());
        await _walletRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WalletNotFound_ReturnsFailure()
    {
        var command = CreateCommand();
        _walletRepo.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

        var result = await _handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Wallet not found");
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task HandleAsync_InsufficientFrozen_ReturnsFailure()
    {
        var command = CreateCommand();
        var wallet = Wallet.Create(command.UserId);
        wallet.Deposit(Money.FromDecimal(500));
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
        result.Error.Should().Contain("Failed to charge winner");
        result.StatusCode.Should().Be(500);
    }
}
