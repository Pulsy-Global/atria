using Atria.Contracts.Events.Feed.Enums;
using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Feed;

public sealed record FeedDeliveryTargetRequest(string Id);

public sealed record OutputUpdated(string Id);

public sealed record FeedOutputData(
    string FeedId,
    List<string>? OutputIds,
    object? Data,
    bool IsTestExecution = false,
    string? BlockNumber = null);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "targetType")]
[JsonDerivedType(typeof(FeedWebhookTarget), "webhook")]
public abstract record FeedDeliveryTarget(string Id, TargetType Type);

public sealed record FeedWebhookTarget(
    string Id,
    string Url,
    string Method = "POST",
    Dictionary<string, string>? Headers = null,
    int TimeoutSeconds = 30)
    : FeedDeliveryTarget(Id, TargetType.Webhook);

public sealed record DeliverTestOutputRequest(string FeedId, List<string>? OutputIds, object? Data);

public sealed record DeliverTestOutputResponse(bool Success, string? Error = null);
