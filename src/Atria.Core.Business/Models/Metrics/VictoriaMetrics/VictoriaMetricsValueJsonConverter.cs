using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atria.Core.Business.Models.Metrics.VictoriaMetrics;

internal sealed class VictoriaMetricsValueJsonConverter : JsonConverter<VictoriaMetricsValue>
{
    public override VictoriaMetricsValue Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray
            || !reader.Read()
            || !reader.TryGetDouble(out var timestamp)
            || !reader.Read()
            || reader.TokenType != JsonTokenType.String
            || !double.TryParse(
                reader.GetString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value)
            || !double.IsFinite(timestamp)
            || !double.IsFinite(value)
            || !reader.Read()
            || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Invalid VictoriaMetrics value.");
        }

        return new VictoriaMetricsValue
        {
            Timestamp = timestamp,
            Value = value,
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        VictoriaMetricsValue value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Timestamp);
        writer.WriteStringValue(value.Value.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndArray();
    }
}
