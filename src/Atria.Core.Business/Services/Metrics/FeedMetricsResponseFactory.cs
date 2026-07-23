using Atria.Core.Business.Models.Metrics;

namespace Atria.Core.Business.Services.Metrics;

internal static class FeedMetricsResponseFactory
{
    public static FeedMetricsDto Create(
        Guid feedId,
        MetricsRangeDefinition range,
        DateTimeOffset generatedAt,
        DateTimeOffset start,
        DateTimeOffset end,
        IReadOnlyDictionary<string, MetricsStoreResult> results)
    {
        var series = FeedMetricSeriesBuilder.Build(
            results,
            start,
            end,
            range.Resolution);
        var status = ResolveStatus(results, series);

        return new FeedMetricsDto
        {
            FeedId = feedId,
            Range = range.Value,
            Status = status,
            GeneratedAt = generatedAt,
            Start = start,
            End = end,
            ResolutionSeconds = (int)range.Resolution.TotalSeconds,
            Summary = FeedMetricsSummaryBuilder.Build(series),
            Series = series,
            Warnings = GetWarnings(status),
        };
    }

    private static string ResolveStatus(
        IReadOnlyDictionary<string, MetricsStoreResult> results,
        IReadOnlyList<FeedMetricSeriesDto> series)
    {
        var succeededGroups = results.Count(pair => pair.Value.Succeeded);
        if (succeededGroups == 0)
        {
            return MetricsAvailability.Unavailable;
        }

        if (succeededGroups < results.Count)
        {
            return MetricsAvailability.Partial;
        }

        var hasActivity = series.Any(item =>
            item.Points.Any(point => point.Value != 0));

        return hasActivity
            ? MetricsAvailability.Available
            : MetricsAvailability.NoData;
    }

    private static IReadOnlyList<string> GetWarnings(string status)
    {
        return status switch
        {
            MetricsAvailability.Partial => ["Some metrics could not be loaded."],
            MetricsAvailability.Unavailable => ["Metrics are temporarily unavailable."],
            _ => [],
        };
    }
}
