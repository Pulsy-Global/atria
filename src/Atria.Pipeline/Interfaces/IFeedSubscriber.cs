using Atria.Common.Messaging.Models;

namespace Atria.Pipeline.Interfaces;

public interface IFeedSubscriber
{
    IAsyncEnumerable<MessagingEnvelope<T>> SubscribeFeedAsync<T>(
        string feedId,
        string consumerName,
        CancellationToken ct = default);

    Task DeleteConsumerAsync(string consumerName, CancellationToken ct = default);
}
