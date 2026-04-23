using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;

namespace Atria.Feed.Delivery.FeedPipeline.Handlers.Delivery;

public interface IDeliveryHandler
{
    TargetType SupportedTargetType { get; }

    Task DeliverAsync(
        string feedId,
        FeedDeliveryTarget target,
        object? data,
        bool isTestExecution,
        CancellationToken ct = default);
}
