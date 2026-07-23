using Atria.Common.Observability;
using Atria.Common.Observability.Models;
using Atria.Feed.Runtime.Engine.Exceptions;
using System.Diagnostics.Metrics;

namespace Atria.Feed.Runtime.Observability;

public sealed class RuntimeMetricsRecorder
{
    private static readonly Counter<long> BlocksProcessed = AtriaMeters.BusinessMetrics.CreateCounter<long>(
        "atria.business.feed.blocks.processed",
        "{event}");
    private static readonly Counter<long> OutputsProduced = AtriaMeters.BusinessMetrics.CreateCounter<long>(
        "atria.business.feed.outputs.produced",
        "{event}");
    private static readonly Counter<long> InputBytes = AtriaMeters.BusinessMetrics.CreateCounter<long>(
        "atria.business.feed.input.bytes",
        "By");
    private static readonly Counter<long> OutputBytes = AtriaMeters.BusinessMetrics.CreateCounter<long>(
        "atria.business.feed.output.bytes",
        "By");
    private static readonly Counter<long> ProcessingFailures = AtriaMeters.BusinessMetrics.CreateCounter<long>(
        "atria.business.feed.processing.failures",
        "{event}");

    private static readonly Histogram<double> ProcessingDuration = AtriaMeters.Observability.CreateHistogram<double>(
        "atria.runtime.block.processing.duration",
        "s");
    private static readonly Counter<long> TerminalFailures = AtriaMeters.Observability.CreateCounter<long>(
        "atria.runtime.block.processing.failures",
        "{event}");
    private static readonly Counter<long> Retries = AtriaMeters.Observability.CreateCounter<long>(
        "atria.runtime.block.processing.retries",
        "{attempt}");
    private static readonly Counter<long> OutputPublishFailures = AtriaMeters.Observability.CreateCounter<long>(
        "atria.runtime.output.publish.failures",
        "{event}");

    public void RecordRetry() => Retries.Add(1, new KeyValuePair<string, object?>("outcome", RuntimeMetricLabels.Failure));

    public void RecordBlockCompleted(
        BusinessMetricScope? scope,
        int? inputSizeBytes,
        TimeSpan duration)
    {
        ProcessingDuration.Record(
            duration.TotalSeconds,
            new KeyValuePair<string, object?>("outcome", RuntimeMetricLabels.Success));

        if (scope == null)
        {
            return;
        }

        var tags = CreateScopeTags(scope);
        BlocksProcessed.Add(1, tags);
        if (inputSizeBytes > 0)
        {
            InputBytes.Add(inputSizeBytes.Value, tags);
        }
    }

    public void RecordOutputPublished(BusinessMetricScope? scope, int? outputSizeBytes)
    {
        if (scope == null)
        {
            return;
        }

        var tags = CreateScopeTags(scope);
        OutputsProduced.Add(1, tags);
        if (outputSizeBytes > 0)
        {
            OutputBytes.Add(outputSizeBytes.Value, tags);
        }
    }

    public void RecordTerminalFailure(BusinessMetricScope? scope, Exception exception, TimeSpan duration)
    {
        var reason = GetFailureReason(exception);
        ProcessingDuration.Record(
            duration.TotalSeconds,
            new KeyValuePair<string, object?>("outcome", RuntimeMetricLabels.Failure));
        TerminalFailures.Add(1, new KeyValuePair<string, object?>("reason", reason));

        if (scope == null)
        {
            return;
        }

        ProcessingFailures.Add(
            1,
            new KeyValuePair<string, object?>("resource_namespace", scope.ResourceNamespace),
            new KeyValuePair<string, object?>("feed_id", scope.FeedId),
            new KeyValuePair<string, object?>("reason", reason));
    }

    public void RecordMissingDataFailure(BusinessMetricScope? scope)
    {
        TerminalFailures.Add(
            1,
            new KeyValuePair<string, object?>("reason", RuntimeMetricLabels.MissingData));

        if (scope == null)
        {
            return;
        }

        ProcessingFailures.Add(
            1,
            new KeyValuePair<string, object?>("resource_namespace", scope.ResourceNamespace),
            new KeyValuePair<string, object?>("feed_id", scope.FeedId),
            new KeyValuePair<string, object?>("reason", RuntimeMetricLabels.MissingData));
    }

    public void RecordOutputPublishFailure() => OutputPublishFailures.Add(1);

    private static KeyValuePair<string, object?>[] CreateScopeTags(BusinessMetricScope scope)
    {
        return
        [
            new KeyValuePair<string, object?>("resource_namespace", scope.ResourceNamespace),
            new KeyValuePair<string, object?>("feed_id", scope.FeedId),
        ];
    }

    private static string GetFailureReason(Exception exception)
    {
        return exception switch
        {
            TimeoutException => RuntimeMetricLabels.Timeout,
            OutputPublishException => RuntimeMetricLabels.PublishError,
            FeedEngineException { IsFunctionError: true } => RuntimeMetricLabels.FunctionError,
            FeedEngineException => RuntimeMetricLabels.FilterError,
            _ => RuntimeMetricLabels.Unknown,
        };
    }
}
