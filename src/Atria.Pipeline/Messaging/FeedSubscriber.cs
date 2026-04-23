using Atria.Common.Messaging.Core;
using Atria.Common.Messaging.Models;
using Atria.Contracts.Subjects.Feed;
using Atria.Pipeline.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using System.Runtime.CompilerServices;

namespace Atria.Pipeline.Messaging;

public sealed class FeedSubscriber : IFeedSubscriber
{
    private readonly NatsJSContext _js;
    private readonly MessagingSettings _settings;
    private readonly StreamManager _streamManager;
    private readonly ILogger<FeedSubscriber> _logger;

    public FeedSubscriber(
        NatsConnectionManager connectionManager,
        StreamManager streamManager,
        IOptions<MessagingSettings> settings,
        ILogger<FeedSubscriber> logger)
    {
        _js = connectionManager.JSContext;
        _streamManager = streamManager;
        _settings = settings.Value;
        _logger = logger;
    }

    public async IAsyncEnumerable<MessagingEnvelope<T>> SubscribeFeedAsync<T>(
        string feedId,
        string consumerName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var subject = FeedSubjects.System.FeedOutput(_settings.JetStreamPrefix, feedId);

        await _streamManager.EnsureStreamAsync(_settings.DefaultFeedStream, ct);

        var consumerConfig = new ConsumerConfig(consumerName)
        {
            FilterSubject = subject,
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            DeliverPolicy = ConsumerConfigDeliverPolicy.All,
            AckWait = TimeSpan.FromSeconds(30),
            MaxAckPending = 1,
            MaxDeliver = 30,
        };

        var consumer = await _js.CreateOrUpdateConsumerAsync(_settings.DefaultFeedStream, consumerConfig, ct);

        _logger.LogInformation(
            "Subscribing to feed results for feed {FeedId}: {Subject} (Consumer={Consumer}, MaxAckPending=1)",
            feedId,
            subject,
            consumerName);

        await foreach (var msg in consumer.ConsumeAsync<T>(cancellationToken: ct))
        {
            if (msg.Data != null)
            {
                yield return new MessagingEnvelope<T>(msg.Data, msg, msg.ReplyTo);
            }
        }
    }

    public async Task DeleteConsumerAsync(string consumerName, CancellationToken ct = default)
    {
        try
        {
            await _js.DeleteConsumerAsync(_settings.DefaultFeedStream, consumerName, ct);
            _logger.LogInformation("Deleted consumer {ConsumerName} from stream {Stream}", consumerName, _settings.DefaultFeedStream);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            _logger.LogDebug("Consumer {ConsumerName} not found, nothing to delete", consumerName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete consumer {ConsumerName}", consumerName);
        }
    }
}
