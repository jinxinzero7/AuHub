using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using AuHub.Shared.ValueObjects;

namespace AuHub.Shared.Converters;

public class MoneyConverter : ValueConverter<Money, decimal>
{
    public MoneyConverter()
        : base(
            money => money.Amount,
            value => Money.FromDecimal(value))
    { }
}
