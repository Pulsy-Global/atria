namespace Atria.Core.Business.Models.Metrics;

public sealed record MetricsStoreSeries(
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyList<FeedMetricPointDto> Points);
