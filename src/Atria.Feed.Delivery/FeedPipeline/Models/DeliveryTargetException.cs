using Atria.Contracts.Events.Feed.Enums;

namespace Atria.Feed.Delivery.FeedPipeline.Models;

public sealed class DeliveryTargetException : Exception
{
    public DeliveryTargetException(TargetType targetType, Exception innerException)
        : base("A feed delivery target failed.", innerException)
    {
        TargetType = targetType;
    }

    public TargetType TargetType { get; }
}
