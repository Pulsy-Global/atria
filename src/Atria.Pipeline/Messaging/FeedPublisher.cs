using Atria.Common.Messaging.Core;
using Atria.Contracts.Subjects.Feed;
using Atria.Pipeline.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.JetStream;

namespace Atria.Pipeline.Messaging;

public sealed class FeedPublisher : IFeedPublisher
{
    private readonly NatsJSContext _js;
    private readonly MessagingSettings _settings;
    private readonly StreamManager _streamManager;
    private readonly ILogger<FeedPublisher> _logger;

    public FeedPublisher(
        NatsConnectionManager connectionManager,
        StreamManager streamManager,
        IOptions<MessagingSettings> settings,
        ILogger<FeedPublisher> logger)
    {
        _js = connectionManager.JSContext;
        _streamManager = streamManager;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task PublishResultAsync<T>(string feedId, T data, CancellationToken ct = default)
    {
        var subject = FeedSubjects.System.FeedOutput(_settings.JetStreamPrefix, feedId);

        await _streamManager.EnsureStreamAsync(_settings.DefaultFeedStream, ct);
        await _js.PublishAsync(subject, data, cancellationToken: ct);

        _logger.LogTrace("Published feed result: {Subject} (FeedId={FeedId})", subject, feedId);
    }
}
