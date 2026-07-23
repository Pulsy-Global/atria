namespace Atria.Core.Business.Models.Metrics;

public sealed record MetricsStoreResult(
    bool Succeeded,
    IReadOnlyList<MetricsStoreSeries> Series)
{
    public static MetricsStoreResult Unavailable { get; } = new(false, []);
}
