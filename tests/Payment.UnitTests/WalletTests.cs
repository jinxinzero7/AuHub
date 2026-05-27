using AuHub.Shared.ValueObjects;
using FluentAssertions;
using Payment.Domain.Entities;

namespace Payment.UnitTests;

public class WalletTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Money Hundred = Money.FromDecimal(100);
    private static readonly Money Fifty = Money.FromDecimal(50);

    [Fact]
    public void Create_SetsProperties()
    {
        var wallet = Wallet.Create(UserId);

        wallet.Id.Should().NotBeEmpty();
        wallet.UserId.Should().Be(UserId);
        wallet.Balance.Should().Be(Money.Zero);
        wallet.FrozenBalance.Should().Be(Money.Zero);
        wallet.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        wallet.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var wallet = Wallet.Create(UserId);
        wallet.Deposit(Hundred);

        wallet.Balance.Should().Be(Hundred);
        wallet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deposit_MultipleTimes_Accumulates()
    {
        var wallet = Wallet.Create(UserId);
        wallet.Deposit(Fifty);
        wallet.Deposit(Hundred);

        wallet.Balance.Should().Be(Money.FromDecimal(150));
    }

    [Fact]
    public void Deposit_WithZeroAmount_Throws()
    {
        var wallet = Wallet.Create(UserId);

        var act = () => wallet.Deposit(Money.Zero);

        act.Should().Throw<InvalidOperationException>().WithMessage("*positive*");
    }

    [Fact]
    public void Withdraw_DecreasesAvailableBalance()
    {
        var wallet = Wallet.Create(UserId);
        wallet.Deposit(Hundred);

        wallet.Withdraw(Fifty);

        wallet.Balance.Should().Be(Fifty);
    }

    [Fact]
    public void Withdraw_InsufficientFunds_Throws()
    {
        var wallet = Wallet.Create(UserId);
        wallet.Deposit(Fifty);

        var act = () => wallet.Withdraw(Hundred);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Insufficient funds*");
    }

    [Fact]
    public void Withdraw_RespectsFrozenBalance()
    {
        var wallet = Wallet.Create(UserId);
        wallet.Deposit(Hundred);
        wallet.Freeze(Fifty);

        var act = () => wallet.Withdraw(Fifty);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Freeze_MovesToFrozenBalance()
    {
        var wallet = Wallet.Create(UserId);
        wallet.Deposit(Hundred);

        wallet.Freeze(Fifty);

        wallet.Balance.Should().Be(Fifty);
        wallet.FrozenBalance.Should().Be(Fifty);
    }

    [Fact]
    public void Freeze_InsufficientBalance_Throws()
    {
        var wallet = Wallet.Create(UserId);

        var act = () => wallet.Freeze(Hundred);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Insufficient funds*");
    }

    [Fact]
    public void Unfreeze_ReturnsToBalance()
    {
        var wallet = Wallet.Create(UserId);
        wallet.Deposit(Hundred);
        wallet.Freeze(Fifty);

        wallet.Unfreeze(Fifty);

        wallet.Balance.Should().Be(Hundred);
        wallet.FrozenBalance.Should().Be(Money.Zero);
    }

    [Fact]
    public void Unfreeze_InsufficientFrozen_Throws()
    {
        var wallet = Wallet.Create(UserId);
        wallet.Deposit(Hundred);

        var act = () => wallet.Unfreeze(Fifty);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Insufficient frozen*");
    }

    [Fact]
    public void TransferFromFrozen_DecreasesFrozen()
    {
        var wallet = Wallet.Create(UserId);
        wallet.Deposit(Hundred);
        wallet.Freeze(Fifty);

        wallet.TransferFromFrozen(Fifty);

        wallet.Balance.Should().Be(Fifty);
        wallet.FrozenBalance.Should().Be(Money.Zero);
    }

    [Fact]
    public void TransferFromFrozen_InsufficientFrozen_Throws()
    {
        var wallet = Wallet.Create(UserId);
        wallet.Deposit(Hundred);

        var act = () => wallet.TransferFromFrozen(Fifty);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Insufficient frozen*");
    }

    [Fact]
    public void FullFlow_DepositFreezeTransferWithdraw_CalculatesCorrectly()
    {
        var wallet = Wallet.Create(UserId);
        wallet.Deposit(Money.FromDecimal(1000));

        wallet.Freeze(Money.FromDecimal(300));
        wallet.Freeze(Money.FromDecimal(200));

        wallet.Unfreeze(Money.FromDecimal(100));

        wallet.TransferFromFrozen(Money.FromDecimal(200));

        wallet.Withdraw(Money.FromDecimal(400));

        wallet.Balance.Should().Be(Money.FromDecimal(200));
        wallet.FrozenBalance.Should().Be(Money.FromDecimal(200));
    }
}
