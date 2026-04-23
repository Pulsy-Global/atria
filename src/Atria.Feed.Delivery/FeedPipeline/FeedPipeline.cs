using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;
using Atria.Feed.Delivery.FeedPipeline.Handlers.Delivery;
using Atria.Feed.Delivery.FeedPipeline.Interfaces;
using Atria.Feed.Delivery.Services;
using Microsoft.Extensions.Logging;

namespace Atria.Feed.Delivery.FeedPipeline;

public class FeedPipeline : IFeedPipeline
{
    private readonly IReadOnlyDictionary<TargetType, IDeliveryHandler> _deliveryHandlers;
    private readonly ILogger<FeedPipeline> _logger;
    private readonly DeliveryConfigService _deliveryConfigService;

    public FeedPipeline(
        DeliveryConfigService deliveryConfigService,
        IEnumerable<IDeliveryHandler> deliveryHandlers,
        ILogger<FeedPipeline> logger)
    {
        _deliveryConfigService = deliveryConfigService;
        _deliveryHandlers = deliveryHandlers.ToDictionary(h => h.SupportedTargetType);
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
        CancellationToken ct = default)
    {
        var currentData = data;

        foreach (var id in outputIds)
        {
            var target = await _deliveryConfigService.TryGetTargetById(id, ct);

            if (target == null)
            {
                _logger.LogError("Delivery config is null");
            }
            else
            {
                await ExecuteFinalDeliveryAsync(currentData, feedId, target, isTestExecution, ct);
            }
        }
    }

    private async Task ExecuteFinalDeliveryAsync(
        object? data,
        string feedId,
        FeedDeliveryTarget deliveryTarget,
        bool isTestExecution,
        CancellationToken ct)
    {
        if (!_deliveryHandlers.TryGetValue(deliveryTarget.Type, out var handler))
        {
            var availableTypes = string.Join(", ", _deliveryHandlers.Keys);
            throw new NotSupportedException(
                $"Delivery target type '{deliveryTarget.Type}' is not supported. Available types: {availableTypes}");
        }

        await handler.DeliverAsync(feedId, deliveryTarget, data, isTestExecution, ct);
    }
}
