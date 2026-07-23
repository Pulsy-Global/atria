using Atria.Common.Observability.Models;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;
using Atria.Feed.Delivery.FeedPipeline.Handlers.Delivery;
using Atria.Feed.Delivery.FeedPipeline.Interfaces;
using Atria.Feed.Delivery.FeedPipeline.Models;
using Atria.Feed.Delivery.Observability;
using Atria.Feed.Delivery.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Atria.Feed.Delivery.FeedPipeline;

public class FeedPipeline : IFeedPipeline
{
    private readonly IReadOnlyDictionary<TargetType, IDeliveryHandler> _deliveryHandlers;
    private readonly ILogger<FeedPipeline> _logger;
    private readonly DeliveryConfigService _deliveryConfigService;
    private readonly DeliveryMetricsRecorder _metrics;

    public FeedPipeline(
        DeliveryConfigService deliveryConfigService,
        IEnumerable<IDeliveryHandler> deliveryHandlers,
        DeliveryMetricsRecorder metrics,
        ILogger<FeedPipeline> logger)
    {
        _deliveryConfigService = deliveryConfigService;
        _deliveryHandlers = deliveryHandlers.ToDictionary(h => h.SupportedTargetType);
        _metrics = metrics;
        _logger = logger;

        _logger.LogInformation(
            "Registered delivery handlers: {DeliveryTypes}",
            string.Join(", ", _deliveryHandlers.Keys));
    }

    public async Task ExecutePipelineAsync(
        string feedId,
        List<string> outputIds,
        object? data,
        bool isTestExecution,
        CancellationToken ct = default,
        string? resourceNamespace = null,
        int? dataSizeBytes = null)
    {
        var currentData = data;
        var metricScope = BusinessMetricScope.Create(resourceNamespace, feedId);

        foreach (var id in outputIds)
        {
            var target = await _deliveryConfigService.TryGetTargetById(id, ct);

            if (target == null)
            {
                var exception = new InvalidDataException($"Delivery target '{id}' could not be resolved.");
                _metrics.RecordFailedAttempt(metricScope, TargetType.None, isTestExecution);
                throw new DeliveryTargetException(TargetType.None, exception);
            }

            await ExecuteFinalDeliveryAsync(
                currentData,
                feedId,
                target,
                isTestExecution,
                metricScope,
                dataSizeBytes,
                ct);
        }
    }

    private async Task ExecuteFinalDeliveryAsync(
        object? data,
        string feedId,
        FeedDeliveryTarget deliveryTarget,
        bool isTestExecution,
        BusinessMetricScope? metricScope,
        int? dataSizeBytes,
        CancellationToken ct)
    {
        if (!_deliveryHandlers.TryGetValue(deliveryTarget.Type, out var handler))
        {
            var availableTypes = string.Join(", ", _deliveryHandlers.Keys);
            var exception = new NotSupportedException(
                $"Delivery target type '{deliveryTarget.Type}' is not supported. Available types: {availableTypes}");
            _metrics.RecordFailedAttempt(metricScope, deliveryTarget.Type, isTestExecution);
            throw new DeliveryTargetException(deliveryTarget.Type, exception);
        }

        var startedAt = Stopwatch.GetTimestamp();
        var succeeded = false;
        var shouldRecordAttempt = false;
        try
        {
            await handler.DeliverAsync(feedId, deliveryTarget, data, isTestExecution, ct);
            succeeded = true;
            shouldRecordAttempt = true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            shouldRecordAttempt = true;
            throw new DeliveryTargetException(deliveryTarget.Type, ex);
        }
        finally
        {
            if (shouldRecordAttempt)
            {
                _metrics.RecordAttempt(
                    metricScope,
                    deliveryTarget.Type,
                    isTestExecution,
                    succeeded,
                    dataSizeBytes,
                    Stopwatch.GetElapsedTime(startedAt));
            }
        }
    }
}
