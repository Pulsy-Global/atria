namespace Atria.Core.Business.Models.Metrics;

public sealed record MetricsRangeDefinition(
    MetricsRange Range,
    string Value,
    TimeSpan Duration,
    TimeSpan Resolution);
