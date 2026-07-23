using System.Text.Json.Serialization;

namespace Atria.Core.Business.Models.Metrics.VictoriaMetrics;

internal sealed class VictoriaMetricsQueryData
{
    [JsonPropertyName("resultType")]
    public string? ResultType { get; init; }

    [JsonPropertyName("result")]
    public IReadOnlyList<VictoriaMetricsSeries>? Result { get; init; }
}
