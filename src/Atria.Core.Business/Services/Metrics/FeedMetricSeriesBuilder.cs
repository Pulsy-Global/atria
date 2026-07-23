using Atria.Core.Business.Models.Metrics;

namespace Atria.Core.Business.Services.Metrics;

internal static class FeedMetricSeriesBuilder
{
    private static readonly IReadOnlyList<FeedMetricSeriesDefinition> Definitions =
    [
        new(
            FeedMetricsCatalog.ProcessedBlocksKey,
            "count",
            FeedMetricsCatalog.RuntimeQueryGroup,
            FeedMetricsCatalog.BlocksProcessed),
        new(
            FeedMetricsCatalog.ProducedOutputsKey,
            "count",
            FeedMetricsCatalog.RuntimeQueryGroup,
            FeedMetricsCatalog.OutputsProduced),
        new(
            FeedMetricsCatalog.ProcessingFailuresKey,
            "count",
            FeedMetricsCatalog.RuntimeQueryGroup,
            FeedMetricsCatalog.ProcessingFailures),
        new(
            FeedMetricsCatalog.ProcessedBytesKey,
            "bytes",
            FeedMetricsCatalog.RuntimeQueryGroup,
            FeedMetricsCatalog.InputBytes),
        new(
            FeedMetricsCatalog.ProducedBytesKey,
            "bytes",
            FeedMetricsCatalog.RuntimeQueryGroup,
            FeedMetricsCatalog.OutputBytes),
        new(
            FeedMetricsCatalog.DeliveryAttemptsKey,
            "count",
            FeedMetricsCatalog.DeliveryAttemptsQueryGroup,
            FeedMetricsCatalog.DeliveryAttempts),
        new(
            FeedMetricsCatalog.SuccessfulDeliveriesKey,
            "count",
            FeedMetricsCatalog.DeliveryAttemptsQueryGroup,
            FeedMetricsCatalog.DeliveryAttempts,
            "success"),
        new(
            FeedMetricsCatalog.DeliveredBytesKey,
            "bytes",
            FeedMetricsCatalog.DeliveryBytesQueryGroup,
            FeedMetricsCatalog.DeliveryBytes),
        new(
            FeedMetricsCatalog.FailedDeliveriesKey,
            "count",
            FeedMetricsCatalog.DeliveryExhaustedQueryGroup,
            FeedMetricsCatalog.DeliveryExhausted),
    ];

    public static IReadOnlyList<FeedMetricSeriesDto> Build(
        IReadOnlyDictionary<string, MetricsStoreResult> results,
        DateTimeOffset start,
        DateTimeOffset end,
        TimeSpan resolution)
    {
        return Definitions
            .Select(definition => BuildSeries(definition, results, start, end, resolution))
            .ToArray();
    }

    private static FeedMetricSeriesDto BuildSeries(
        FeedMetricSeriesDefinition definition,
        IReadOnlyDictionary<string, MetricsStoreResult> results,
        DateTimeOffset start,
        DateTimeOffset end,
        TimeSpan resolution)
    {
        var result = results[definition.QueryGroup];
        if (!result.Succeeded)
        {
            return new FeedMetricSeriesDto(
                definition.Key,
                definition.Unit,
                MetricsAvailability.Unavailable,
                []);
        }

        var valuesByTimestamp = result.Series
            .Where(series => Matches(series, definition))
            .SelectMany(series => series.Points)
            .GroupBy(point => point.Timestamp.ToUnixTimeSeconds())
            .ToDictionary(group => group.Key, group => group.Sum(point => point.Value));
        var points = BuildAlignedPoints(valuesByTimestamp, start, end, resolution);

        return new FeedMetricSeriesDto(
            definition.Key,
            definition.Unit,
            MetricsAvailability.Available,
            points);
    }

    private static bool Matches(
        MetricsStoreSeries series,
        FeedMetricSeriesDefinition definition)
    {
        if (!series.Labels.TryGetValue("__name__", out var metricName)
            || metricName != definition.MetricName)
        {
            return false;
        }

        return definition.Outcome == null
            || (series.Labels.TryGetValue("outcome", out var outcome)
                && outcome == definition.Outcome);
    }

    private static IReadOnlyList<FeedMetricPointDto> BuildAlignedPoints(
        IReadOnlyDictionary<long, double> valuesByTimestamp,
        DateTimeOffset start,
        DateTimeOffset end,
        TimeSpan resolution)
    {
        var points = new List<FeedMetricPointDto>();
        for (var timestamp = start + resolution; timestamp <= end; timestamp += resolution)
        {
            valuesByTimestamp.TryGetValue(timestamp.ToUnixTimeSeconds(), out var value);
            points.Add(new FeedMetricPointDto(timestamp, value));
        }

        return points;
    }
}
