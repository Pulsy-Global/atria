namespace Atria.Feed.Delivery.Config.Options;

public class FeedDeliveryOptions
{
    public const string SectionName = "FeedDelivery";

    public string UserAgent { get; set; } = "Pulsy-Atria/1.0";

    public DeliveryConfigCacheOptions ConfigCache { get; set; } = new();
}
