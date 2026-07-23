namespace Atria.Core.Business.Models.Metrics;

public sealed record FeedMetricPointDto(DateTimeOffset Timestamp, double Value);
