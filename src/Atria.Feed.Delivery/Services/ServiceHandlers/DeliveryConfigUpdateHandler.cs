using Atria.Common.Messaging.ServiceBus;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Subjects.Feed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Atria.Feed.Delivery.Services.ServiceHandlers;

public sealed class DeliveryConfigUpdateHandler : ServiceBusHandler<OutputUpdated>
{
    private readonly IMemoryCache _cache;

    public DeliveryConfigUpdateHandler(
        IServiceBus serviceBus,
        IMemoryCache cache,
        ILogger<DeliveryConfigUpdateHandler> logger)
        : base(serviceBus, logger)
    {
        _cache = cache;
    }

    protected override string Subject => FeedSubjects.System.DeliveryConfigUpdated;

    protected override string? QueueGroup => null;

    protected override Task HandleAsync(OutputUpdated message, CancellationToken ct)
    {
        var key = DeliveryConfigService.GetCacheKey(message.Id);
        _cache.Remove(key);

        Logger.LogInformation("Invalidated cache for output {OutputId}", message.Id);
        return Task.CompletedTask;
    }
}
