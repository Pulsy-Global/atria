using Atria.Contracts.Events.Feed.Enums;

namespace Atria.Feed.Delivery.Observability;

public static class DeliveryMetricLabels
{
    public const string Success = "success";
    public const string Failure = "failure";
    public const string Live = "live";
    public const string Test = "test";
    public const string Webhook = "webhook";
    public const string Unknown = "unknown";

    public static string GetTargetType(TargetType targetType)
    {
        return targetType switch
        {
            TargetType.Webhook => Webhook,
            _ => Unknown,
        };
    }
}
