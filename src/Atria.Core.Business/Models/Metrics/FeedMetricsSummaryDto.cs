using Newtonsoft.Json;

namespace Atria.Core.Business.Models.Metrics;

public sealed class FeedMetricsSummaryDto
{
    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public double? ProcessedBlocks { get; init; }

    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public double? ProducedOutputs { get; init; }

    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public double? ProcessingFailures { get; init; }

    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public double? DeliveryAttempts { get; init; }

    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public double? SuccessfulDeliveries { get; init; }

    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public double? FailedDeliveries { get; init; }

    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public double? DeliverySuccessRate { get; init; }

    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public double? DataReductionRate { get; init; }

    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public double? ProcessedBytes { get; init; }

    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public double? ProducedBytes { get; init; }

    [JsonProperty(NullValueHandling = NullValueHandling.Include)]
    public double? DeliveredBytes { get; init; }
}
