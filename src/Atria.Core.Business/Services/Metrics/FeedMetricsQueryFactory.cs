using Atria.Core.Business.Models.Metrics;

namespace Atria.Core.Business.Services.Metrics;

public static class FeedMetricsQueryFactory
{
    private static readonly string RuntimeMetricNamesPattern = string.Join(
        "|",
        [
            FeedMetricsCatalog.BlocksProcessed,
            FeedMetricsCatalog.OutputsProduced,
            FeedMetricsCatalog.InputBytes,
            FeedMetricsCatalog.OutputBytes,
            FeedMetricsCatalog.ProcessingFailures,
        ]);

    public static IReadOnlyList<FeedMetricsQueryGroup> Create(
        FeedMetricsScope scope,
        DateTimeOffset start,
        DateTimeOffset end,
        TimeSpan resolution)
    {
        var labels = $"resource_namespace=\"{Escape(scope.ResourceNamespace)}\",feed_id=\"{scope.FeedId}\"";
        var window = $"{(int)resolution.TotalSeconds}s";

        return
        [
            CreateGroup(
                FeedMetricsCatalog.RuntimeQueryGroup,
                $"sum by (__name__, reason) (increase({{__name__=~\"({RuntimeMetricNamesPattern})\",{labels}}}[{window}]) keep_metric_names)",
                start,
                end,
                resolution),
            CreateGroup(
                FeedMetricsCatalog.DeliveryAttemptsQueryGroup,
                $"sum by (__name__, outcome, target_type) (increase({FeedMetricsCatalog.DeliveryAttempts}{{{labels}}}[{window}]) keep_metric_names)",
                start,
                end,
                resolution),
            CreateGroup(
                FeedMetricsCatalog.DeliveryBytesQueryGroup,
                $"sum by (__name__, target_type) (increase({FeedMetricsCatalog.DeliveryBytes}{{{labels}}}[{window}]) keep_metric_names)",
                start,
                end,
                resolution),
            CreateGroup(
                FeedMetricsCatalog.DeliveryExhaustedQueryGroup,
                $"sum by (__name__, target_type) (increase({FeedMetricsCatalog.DeliveryExhausted}{{{labels}}}[{window}]) keep_metric_names)",
                start,
                end,
                resolution),
        ];
    }

    private static FeedMetricsQueryGroup CreateGroup(
        string key,
        string expression,
        DateTimeOffset start,
        DateTimeOffset end,
        TimeSpan resolution)
    {
        return new FeedMetricsQueryGroup(
            key,
            new MetricsStoreQuery(expression, start + resolution, end, resolution));
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
