using Atria.Core.Business.Models.Metrics;

namespace Atria.Core.Business.Services.Metrics;

internal static class FeedMetricsSummaryBuilder
{
    public static FeedMetricsSummaryDto Build(
        IReadOnlyList<FeedMetricSeriesDto> series)
    {
        var values = series.ToDictionary(item => item.Key, SumAvailableSeries);
        var deliveryAttempts = values[FeedMetricsCatalog.DeliveryAttemptsKey];
        var successfulDeliveries = values[FeedMetricsCatalog.SuccessfulDeliveriesKey];
        var processedBytes = values[FeedMetricsCatalog.ProcessedBytesKey];
        var producedBytes = values[FeedMetricsCatalog.ProducedBytesKey];

        return new FeedMetricsSummaryDto
        {
            ProcessedBlocks = values[FeedMetricsCatalog.ProcessedBlocksKey],
            ProducedOutputs = values[FeedMetricsCatalog.ProducedOutputsKey],
            ProcessingFailures = values[FeedMetricsCatalog.ProcessingFailuresKey],
            DeliveryAttempts = deliveryAttempts,
            SuccessfulDeliveries = successfulDeliveries,
            FailedDeliveries = values[FeedMetricsCatalog.FailedDeliveriesKey],
            DeliverySuccessRate = CalculateSuccessRate(deliveryAttempts, successfulDeliveries),
            DataReductionRate = CalculateDataReductionRate(processedBytes, producedBytes),
            ProcessedBytes = processedBytes,
            ProducedBytes = producedBytes,
            DeliveredBytes = values[FeedMetricsCatalog.DeliveredBytesKey],
        };
    }

    private static double? SumAvailableSeries(FeedMetricSeriesDto series)
    {
        return series.Status == MetricsAvailability.Available
            ? series.Points.Sum(point => point.Value)
            : null;
    }

    private static double? CalculateSuccessRate(double? attempts, double? successes)
    {
        if (attempts == null || successes == null)
        {
            return null;
        }

        return attempts == 0
            ? 0
            : Math.Round(successes.Value / attempts.Value * 100, 2);
    }

    private static double? CalculateDataReductionRate(
        double? processedBytes,
        double? producedBytes)
    {
        if (processedBytes == null || producedBytes == null || processedBytes == 0)
        {
            return null;
        }

        return Math.Round((1 - (producedBytes.Value / processedBytes.Value)) * 100, 2);
    }
}
