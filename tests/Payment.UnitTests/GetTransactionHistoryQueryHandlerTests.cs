using AuHub.Shared.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Payment.Application.Queries.GetTransactionHistory;
using Payment.Application.Repositories;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.UnitTests;

public class GetTransactionHistoryQueryHandlerTests
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly GetTransactionHistoryQueryHandler _handler;

    public GetTransactionHistoryQueryHandlerTests()
    {
        _transactionRepository = Substitute.For<ITransactionRepository>();
        _handler = new GetTransactionHistoryQueryHandler(_transactionRepository);
    }

    [Fact]
    public async Task HandleAsync_MapsWalletEffect()
    {
        var userId = Guid.NewGuid();
        var referenceId = Guid.NewGuid();
        var transaction = Transaction.Create(
            userId,
            TransactionType.Reserve,
            Money.FromDecimal(500m),
            "Reserve funds",
            referenceId);

        _transactionRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Transaction> { transaction });

        var result = await _handler.HandleAsync(new GetTransactionHistoryQuery { UserId = userId });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Transactions.Should().ContainSingle();
        result.Value.Transactions[0].Effect.Should().Be("Freeze");
    }
}
