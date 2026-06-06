using System.Globalization;
using System.Text.Json.Serialization;
using AuHub.Shared.Converters;

namespace AuHub.Shared.ValueObjects;

[JsonConverter(typeof(MoneyJsonConverter))]
public sealed record Money : IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money FromDecimal(decimal amount, string currency = "RUB")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        return new Money(amount, currency);
    }

    public static readonly Money Zero = new(0, "RUB");

    public bool IsZero => Amount == 0;

    public int CompareTo(Money? other) => Amount.CompareTo(other?.Amount);

    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount, a.Currency);
    public static Money operator -(Money a, Money b) => new(a.Amount - b.Amount, a.Currency);
    public static Money operator *(Money a, decimal multiplier) => new(a.Amount * multiplier, a.Currency);
    public static Money operator *(decimal multiplier, Money a) => new(a.Amount * multiplier, a.Currency);
    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
    public static bool operator >=(Money a, Money b) => a.Amount >= b.Amount;
    public static bool operator <=(Money a, Money b) => a.Amount <= b.Amount;

    public override string ToString() => $"{Amount.ToString("N2", CultureInfo.InvariantCulture)} {Currency}";
}
