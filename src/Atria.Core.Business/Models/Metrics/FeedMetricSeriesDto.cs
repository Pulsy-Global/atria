namespace Atria.Core.Business.Models.Metrics;

public sealed record FeedMetricSeriesDto(
    string Key,
    string Unit,
    string Status,
    IReadOnlyList<FeedMetricPointDto> Points);
