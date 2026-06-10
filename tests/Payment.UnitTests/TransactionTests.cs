using AuHub.Shared.ValueObjects;
using FluentAssertions;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.UnitTests;

public class TransactionTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Money Hundred = Money.FromDecimal(100);

    [Fact]
    public void Create_SetsProperties()
    {
        var transaction = Transaction.Create(UserId, TransactionType.Deposit, Hundred, "Test deposit");

        transaction.Id.Should().NotBeEmpty();
        transaction.UserId.Should().Be(UserId);
        transaction.Type.Should().Be(TransactionType.Deposit);
        transaction.Amount.Should().Be(Hundred);
        transaction.Description.Should().Be("Test deposit");
        transaction.ReferenceId.Should().BeNull();
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithReferenceId_SetsReference()
    {
        var referenceId = Guid.NewGuid();

        var transaction = Transaction.Create(UserId, TransactionType.Reserve, Hundred, "Reserve", referenceId);

        transaction.ReferenceId.Should().Be(referenceId);
    }

    [Fact]
    public void Create_WithDifferentTypes_SetsType()
    {
        var types = new[]
        {
            TransactionType.Deposit,
            TransactionType.Withdraw,
            TransactionType.Reserve,
            TransactionType.Release,
            TransactionType.Win,
            TransactionType.Transfer,
            TransactionType.Refund,
            TransactionType.DisputeRefund,
            TransactionType.ServiceFee
        };

        foreach (var type in types)
        {
            var transaction = Transaction.Create(UserId, type, Hundred, "Test");
            transaction.Type.Should().Be(type);
        }
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var t1 = Transaction.Create(UserId, TransactionType.Deposit, Hundred, "Test");
        var t2 = Transaction.Create(UserId, TransactionType.Deposit, Hundred, "Test");

        t1.Id.Should().NotBe(t2.Id);
    }
}
