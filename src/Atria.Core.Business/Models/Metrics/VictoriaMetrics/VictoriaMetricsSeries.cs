using System.Text.Json.Serialization;

namespace Atria.Core.Business.Models.Metrics.VictoriaMetrics;

internal sealed class VictoriaMetricsSeries
{
    [JsonPropertyName("metric")]
    public IReadOnlyDictionary<string, string>? Metric { get; init; }

    [JsonPropertyName("values")]
    public IReadOnlyList<VictoriaMetricsValue>? Values { get; init; }
}
