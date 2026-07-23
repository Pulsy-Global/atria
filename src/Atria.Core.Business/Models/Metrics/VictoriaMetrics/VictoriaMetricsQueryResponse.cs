using System.Text.Json.Serialization;

namespace Atria.Core.Business.Models.Metrics.VictoriaMetrics;

internal sealed class VictoriaMetricsQueryResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("data")]
    public VictoriaMetricsQueryData? Data { get; init; }
}
