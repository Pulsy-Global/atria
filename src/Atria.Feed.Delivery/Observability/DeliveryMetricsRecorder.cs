using Atria.Common.Observability;
using Atria.Common.Observability.Models;
using Atria.Contracts.Events.Feed.Enums;
using System.Diagnostics.Metrics;

namespace Atria.Feed.Delivery.Observability;

public sealed class DeliveryMetricsRecorder
{
    private static readonly Counter<long> DeliveryAttempts = AtriaMeters.BusinessMetrics.CreateCounter<long>(
        "atria.business.feed.delivery.attempts",
        "{attempt}");
    private static readonly Counter<long> DeliveryBytes = AtriaMeters.BusinessMetrics.CreateCounter<long>(
        "atria.business.feed.delivery.bytes",
        "By");
    private static readonly Counter<long> DeliveryExhausted = AtriaMeters.BusinessMetrics.CreateCounter<long>(
        "atria.business.feed.delivery.exhausted",
        "{event}");
    private static readonly Histogram<double> TargetDuration = AtriaMeters.Observability.CreateHistogram<double>(
        "atria.delivery.target.duration",
        "s");
    private static readonly Counter<long> MessageHandling = AtriaMeters.Observability.CreateCounter<long>(
        "atria.delivery.message.handling",
        "{event}");
    private static readonly Counter<long> MessageRetries = AtriaMeters.Observability.CreateCounter<long>(
        "atria.delivery.message.retries",
        "{attempt}");

    private int _activeConsumers;

    public DeliveryMetricsRecorder()
    {
        AtriaMeters.Observability.CreateObservableGauge(
            "atria.delivery.consumers.active",
            () => Volatile.Read(ref _activeConsumers),
            "{item}");
    }

    public void ConsumerStarted() => Interlocked.Increment(ref _activeConsumers);

    public void ConsumerStopped() => Interlocked.Decrement(ref _activeConsumers);

    public void RecordAttempt(
        BusinessMetricScope? scope,
        TargetType targetType,
        bool isTestExecution,
        bool succeeded,
        int? dataSizeBytes,
        TimeSpan duration)
    {
        var target = DeliveryMetricLabels.GetTargetType(targetType);
        var outcome = succeeded ? DeliveryMetricLabels.Success : DeliveryMetricLabels.Failure;
        var mode = isTestExecution ? DeliveryMetricLabels.Test : DeliveryMetricLabels.Live;

        TargetDuration.Record(
            duration.TotalSeconds,
            new KeyValuePair<string, object?>("target_type", target),
            new KeyValuePair<string, object?>("mode", mode),
            new KeyValuePair<string, object?>("outcome", outcome));

        RecordBusinessAttempt(scope, targetType, isTestExecution, succeeded);

        if (!isTestExecution && scope != null && succeeded && dataSizeBytes > 0)
        {
            DeliveryBytes.Add(
                dataSizeBytes.Value,
                new KeyValuePair<string, object?>("resource_namespace", scope.ResourceNamespace),
                new KeyValuePair<string, object?>("feed_id", scope.FeedId),
                new KeyValuePair<string, object?>("target_type", target));
        }
    }

    public void RecordFailedAttempt(
        BusinessMetricScope? scope,
        TargetType targetType,
        bool isTestExecution)
        => RecordBusinessAttempt(scope, targetType, isTestExecution, succeeded: false);

    public void RecordMessageHandled(bool succeeded)
    {
        MessageHandling.Add(
            1,
            new KeyValuePair<string, object?>(
                "outcome",
                succeeded ? DeliveryMetricLabels.Success : DeliveryMetricLabels.Failure));
    }

    public void RecordRetry() => MessageRetries.Add(
        1,
        new KeyValuePair<string, object?>("outcome", DeliveryMetricLabels.Failure));

    public void RecordExhausted(BusinessMetricScope? scope, TargetType targetType, bool isTestExecution)
    {
        if (isTestExecution || scope == null)
        {
            return;
        }

        DeliveryExhausted.Add(
            1,
            new KeyValuePair<string, object?>("resource_namespace", scope.ResourceNamespace),
            new KeyValuePair<string, object?>("feed_id", scope.FeedId),
            new KeyValuePair<string, object?>("target_type", DeliveryMetricLabels.GetTargetType(targetType)));
    }

    private static void RecordBusinessAttempt(
        BusinessMetricScope? scope,
        TargetType targetType,
        bool isTestExecution,
        bool succeeded)
    {
        if (isTestExecution || scope == null)
        {
            return;
        }

        DeliveryAttempts.Add(
            1,
            new KeyValuePair<string, object?>("resource_namespace", scope.ResourceNamespace),
            new KeyValuePair<string, object?>("feed_id", scope.FeedId),
            new KeyValuePair<string, object?>("target_type", DeliveryMetricLabels.GetTargetType(targetType)),
            new KeyValuePair<string, object?>(
                "outcome",
                succeeded ? DeliveryMetricLabels.Success : DeliveryMetricLabels.Failure));
    }
}
