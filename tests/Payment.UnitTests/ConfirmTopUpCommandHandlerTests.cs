using AuHub.Shared.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Payment.Application.Commands.ConfirmTopUp;
using Payment.Application.Repositories;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.UnitTests;

public class ConfirmTopUpCommandHandlerTests
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ConfirmTopUpCommandHandler _handler;

    public ConfirmTopUpCommandHandlerTests()
    {
        _walletRepository = Substitute.For<IWalletRepository>();
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _handler = new ConfirmTopUpCommandHandler(_walletRepository, _transactionRepository);
    }

    [Fact]
    public async Task HandleAsync_NewProviderOperation_DepositsOnce()
    {
        var command = CreateCommand();
        _walletRepository.GetByUserIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((Wallet?)null);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        await _walletRepository.Received(1).AddAsync(Arg.Is<Wallet>(wallet =>
            wallet.UserId == command.UserId &&
            wallet.Balance == command.Amount), Arg.Any<CancellationToken>());
        await _transactionRepository.Received(1).AddAsync(Arg.Is<Transaction>(transaction =>
            transaction.UserId == command.UserId &&
            transaction.Type == TransactionType.Deposit &&
            transaction.Amount == command.Amount &&
            transaction.ReferenceId == command.OperationId), Arg.Any<CancellationToken>());
        await _walletRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateProviderOperation_DoesNotDepositAgain()
    {
        var command = CreateCommand();
        var transaction = Transaction.Create(
            command.UserId,
            TransactionType.Deposit,
            command.Amount,
            "Confirmed Robokassa top-up",
            command.OperationId);
        _transactionRepository
            .GetByUserIdTypeAndReferenceIdAsync(command.UserId, TransactionType.Deposit, command.OperationId, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        await _walletRepository.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _transactionRepository.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    private static ConfirmTopUpCommand CreateCommand()
    {
        return new ConfirmTopUpCommand
        {
            UserId = Guid.NewGuid(),
            OperationId = Guid.NewGuid(),
            Amount = Money.FromDecimal(500m),
            Provider = "Robokassa"
        };
    }
}
