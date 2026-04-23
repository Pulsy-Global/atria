using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.JetStream;
using System.Collections.Concurrent;
using JsStreamConfig = NATS.Client.JetStream.Models.StreamConfig;

namespace Atria.Common.Messaging.Core;

public sealed class StreamManager
{
    private readonly NatsJSContext _js;
    private readonly MessagingSettings _settings;
    private readonly ILogger<StreamManager> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, bool> _ensuredStreams = new();

    public StreamManager(
        NatsConnectionManager connectionManager,
        IOptions<MessagingSettings> settings,
        ILogger<StreamManager> logger)
    {
        _js = connectionManager.JSContext;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task EnsureStreamAsync(string streamName, CancellationToken ct = default)
    {
        if (_ensuredStreams.ContainsKey(streamName))
        {
            return;
        }

        await _semaphore.WaitAsync(ct);
        try
        {
            if (_ensuredStreams.ContainsKey(streamName))
            {
                return;
            }

            var config = ResolveStreamConfig(streamName);

            _logger.LogInformation("Ensuring stream '{StreamName}' exists", streamName);

            var streamConfig = new JsStreamConfig(streamName, config.Subjects)
            {
                MaxBytes = config.GetMaxSizeBytes(),
                MaxAge = config.GetMaxAge(),
                MaxMsgs = config.MaxMessages > 0 ? config.MaxMessages : 0,
                MaxMsgsPerSubject = config.MaxMessagesPerSubject > 0 ? config.MaxMessagesPerSubject : 0,
                Discard = config.DiscardPolicy,
                DiscardNewPerSubject = config.DiscardNewPerSubject,
                NumReplicas = config.Replicas,
            };

            try
            {
                await _js.CreateOrUpdateStreamAsync(streamConfig, cancellationToken: ct);
            }
            catch (NatsJSApiException ex) when (ex.Error.Code == 400)
            {
                _logger.LogWarning(
                    "Stream '{StreamName}' already exists with overlapping subjects, using existing stream",
                    streamName);
            }

            _ensuredStreams.TryAdd(streamName, true);
            _logger.LogInformation(
                "Stream {StreamName} ensured (MaxSize={MaxSize}, MaxAge={MaxAge}min, Replicas={Replicas})",
                streamName,
                config.MaxSizeMb,
                config.MaxAgeMinutes,
                config.Replicas);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private MessagingStreamConfig ResolveStreamConfig(string streamName)
    {
        if (_settings.Streams.TryGetValue(streamName, out var streamConfig))
        {
            return streamConfig;
        }

        if (_settings.DefaultStream != null)
        {
            var config = new MessagingStreamConfig
            {
                Name = streamName,
                MaxSizeMb = _settings.DefaultStream.MaxSizeMb,
                MaxAgeMinutes = _settings.DefaultStream.MaxAgeMinutes,
                MaxMessages = _settings.DefaultStream.MaxMessages,
                MaxMessagesPerSubject = _settings.DefaultStream.MaxMessagesPerSubject,
                DiscardPolicy = _settings.DefaultStream.DiscardPolicy,
                DiscardNewPerSubject = _settings.DefaultStream.DiscardNewPerSubject,
                Replicas = _settings.DefaultStream.Replicas,
            };

            if (config.Subjects.Length == 0)
            {
                config.Subjects = new[] { $"{streamName}.>" };
            }

            return config;
        }

        return new MessagingStreamConfig
        {
            Name = streamName,
            Subjects = new[] { $"{streamName}.>" },
        };
    }
}
