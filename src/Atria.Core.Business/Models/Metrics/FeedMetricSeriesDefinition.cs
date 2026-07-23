namespace Atria.Core.Business.Models.Metrics;

internal sealed record FeedMetricSeriesDefinition(
    string Key,
    string Unit,
    string QueryGroup,
    string MetricName,
    string? Outcome = null);
