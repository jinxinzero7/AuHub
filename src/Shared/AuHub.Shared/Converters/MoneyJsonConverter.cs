using System.Text.Json;
using System.Text.Json.Serialization;
using AuHub.Shared.ValueObjects;

namespace AuHub.Shared.Converters;

public class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var amount = reader.GetDecimal();
            return Money.FromDecimal(amount);
        }
        throw new JsonException($"Expected number but got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Amount);
    }
}
