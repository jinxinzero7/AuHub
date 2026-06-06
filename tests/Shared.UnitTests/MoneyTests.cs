using AuHub.Shared.ValueObjects;
using FluentAssertions;

namespace Shared.UnitTests;

public class MoneyTests
{
    [Fact]
    public void FromDecimal_WithValidAmount_CreatesMoney()
    {
        var money = Money.FromDecimal(100.50m);
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("RUB");
    }

    [Fact]
    public void FromDecimal_WithCustomCurrency_SetsCurrency()
    {
        var money = Money.FromDecimal(50m, "USD");
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void FromDecimal_WithNegativeAmount_Throws()
    {
        var act = () => Money.FromDecimal(-1m);
        act.Should().Throw<ArgumentException>().WithParameterName("amount");
    }

    [Fact]
    public void Zero_ReturnsMoneyWithZeroAmount()
    {
        Money.Zero.Amount.Should().Be(0);
        Money.Zero.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Addition_AddsAmounts()
    {
        var a = Money.FromDecimal(100m);
        var b = Money.FromDecimal(50m);
        (a + b).Should().Be(Money.FromDecimal(150m));
    }

    [Fact]
    public void Subtraction_SubtractsAmounts()
    {
        var a = Money.FromDecimal(100m);
        var b = Money.FromDecimal(30m);
        (a - b).Should().Be(Money.FromDecimal(70m));
    }

    [Fact]
    public void MultiplyByDecimal_MultipliesAmount()
    {
        var money = Money.FromDecimal(100m);
        (money * 0.1m).Should().Be(Money.FromDecimal(10m));
    }

    [Fact]
    public void DecimalMultiplyByMoney_MultipliesAmount()
    {
        var money = Money.FromDecimal(100m);
        (0.5m * money).Should().Be(Money.FromDecimal(50m));
    }

    [Theory]
    [InlineData(100, 50, true)]
    [InlineData(50, 100, false)]
    [InlineData(100, 100, false)]
    public void GreaterThan_ComparesCorrectly(decimal a, decimal b, bool expected)
    {
        (Money.FromDecimal(a) > Money.FromDecimal(b)).Should().Be(expected);
    }

    [Theory]
    [InlineData(50, 100, true)]
    [InlineData(100, 50, false)]
    [InlineData(100, 100, false)]
    public void LessThan_ComparesCorrectly(decimal a, decimal b, bool expected)
    {
        (Money.FromDecimal(a) < Money.FromDecimal(b)).Should().Be(expected);
    }

    [Theory]
    [InlineData(100, 100, true)]
    [InlineData(100, 50, true)]
    [InlineData(50, 100, false)]
    public void GreaterThanOrEqual_ComparesCorrectly(decimal a, decimal b, bool expected)
    {
        (Money.FromDecimal(a) >= Money.FromDecimal(b)).Should().Be(expected);
    }

    [Theory]
    [InlineData(100, 100, true)]
    [InlineData(50, 100, true)]
    [InlineData(100, 50, false)]
    public void LessThanOrEqual_ComparesCorrectly(decimal a, decimal b, bool expected)
    {
        (Money.FromDecimal(a) <= Money.FromDecimal(b)).Should().Be(expected);
    }

    [Fact]
    public void CompareTo_ReturnsZero_ForEqualAmounts()
    {
        Money.FromDecimal(100m).CompareTo(Money.FromDecimal(100m)).Should().Be(0);
    }

    [Fact]
    public void CompareTo_ReturnsPositive_ForGreaterAmount()
    {
        Money.FromDecimal(100m).CompareTo(Money.FromDecimal(50m)).Should().BePositive();
    }

    [Fact]
    public void CompareTo_ReturnsNegative_ForLesserAmount()
    {
        Money.FromDecimal(50m).CompareTo(Money.FromDecimal(100m)).Should().BeNegative();
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        Money.FromDecimal(100.50m).ToString().Should().Be("100.50 RUB");
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var a = Money.FromDecimal(100m);
        var b = Money.FromDecimal(100m);
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_DifferentAmount_AreNotEqual()
    {
        var a = Money.FromDecimal(100m);
        var b = Money.FromDecimal(200m);
        a.Should().NotBe(b);
    }
}
