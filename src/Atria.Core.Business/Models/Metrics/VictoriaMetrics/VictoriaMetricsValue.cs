using System.Text.Json.Serialization;

namespace Atria.Core.Business.Models.Metrics.VictoriaMetrics;

[JsonConverter(typeof(VictoriaMetricsValueJsonConverter))]
internal sealed class VictoriaMetricsValue
{
    public double Timestamp { get; init; }

    public double Value { get; init; }
}
