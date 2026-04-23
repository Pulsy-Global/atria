using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using NATS.Net;
using System.Collections.Concurrent;

namespace Atria.Common.Messaging.Core;

public sealed class NatsConnectionManager : IHostedService, IAsyncDisposable
{
    private readonly MessagingSettings _settings;
    private readonly ILogger<NatsConnectionManager> _logger;
    private readonly SemaphoreSlim _kvLock = new(1, 1);
    private readonly NatsConnection _connection;
    private readonly NatsJSContext _jsContext;
    private readonly string _serviceName;
    private readonly ConcurrentDictionary<string, INatsKVStore> _kvStores = new();

    public NatsConnectionManager(
        IOptions<MessagingSettings> settings,
        ILogger<NatsConnectionManager> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        _serviceName = string.IsNullOrEmpty(_settings.ServiceName)
            ? "Atria.Messaging"
            : _settings.ServiceName;

        var opts = new NatsOpts
        {
            Url = _settings.Url,
            AuthOpts = new NatsAuthOpts
            {
                Username = _settings.Username,
                Password = _settings.Password,
            },
            Name = _serviceName,
            SerializerRegistry = NatsSerializerRegistry.Default,
        };

        _connection = new NatsConnection(opts);
        _jsContext = new NatsJSContext(_connection);

        _logger.LogInformation("NATS connection manager created for {ServiceName}", _serviceName);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("Connecting to NATS at {Url}...", _settings.Url);
        await _connection.ConnectAsync();
        _logger.LogInformation("NATS connected successfully for {ServiceName}", _serviceName);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    public NatsConnection Connection => _connection;

    public NatsJSContext JSContext => _jsContext;

    public async ValueTask<INatsKVStore> GetKVStoreAsync(string? bucketName = null, CancellationToken ct = default)
        => await GetKVStoreAsync(bucketName, defaultConfig: null, ct);

    public async ValueTask<INatsKVStore> GetKVStoreAsync(
        string? bucketName,
        MessagingKVConfig? defaultConfig,
        CancellationToken ct = default)
    {
        var targetBucket = bucketName ?? _settings.KVBucketName;

        if (_kvStores.TryGetValue(targetBucket, out var store))
        {
            return store;
        }

        await _kvLock.WaitAsync(ct);
        try
        {
            if (_kvStores.TryGetValue(targetBucket, out store))
            {
                return store;
            }

            var kvContext = _connection.CreateKeyValueStoreContext();

            NatsKVConfig config;
            if (_settings.KVs.TryGetValue(targetBucket, out var kvConfig))
            {
                config = new NatsKVConfig(targetBucket)
                {
                    MaxAge = GetMaxAge(kvConfig),
                    History = kvConfig.History,
                    NumberOfReplicas = kvConfig.Replicas,
                    Storage = NatsKVStorageType.File,
                    MaxBytes = (kvConfig.MaxSizeMb ?? 0) * 1024 * 1024,
                    Compression = kvConfig.Compression,
                };
            }
            else if (defaultConfig is not null)
            {
                config = new NatsKVConfig(targetBucket)
                {
                    MaxAge = GetMaxAge(defaultConfig),
                    History = defaultConfig.History,
                    NumberOfReplicas = defaultConfig.Replicas,
                    Storage = NatsKVStorageType.File,
                    MaxBytes = (defaultConfig.MaxSizeMb ?? 0) * 1024 * 1024,
                    Compression = defaultConfig.Compression,
                };
            }
            else
            {
                config = new NatsKVConfig(targetBucket)
                {
                    MaxAge = TimeSpan.FromHours(24),
                    History = 1,
                    NumberOfReplicas = 1,
                    Storage = NatsKVStorageType.File,
                };
            }

            store = await kvContext.CreateStoreAsync(config, cancellationToken: ct);

            _kvStores.TryAdd(targetBucket, store);
            _logger.LogInformation(
                "KV Store '{Bucket}' initialized. MaxAge: {MaxAge}, History: {History}, Replicas: {Replicas}, Compression: {Compression}",
                targetBucket,
                config.MaxAge,
                config.History,
                config.NumberOfReplicas,
                config.Compression);

            return store;
        }
        finally
        {
            _kvLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _kvLock.Dispose();
        await _connection.DisposeAsync();
    }

    private static TimeSpan GetMaxAge(MessagingKVConfig config)
    {
        if (config.MaxAgeSeconds > 0)
        {
            return TimeSpan.FromSeconds(config.MaxAgeSeconds);
        }

        if (config.MaxAgeMinutes > 0)
        {
            return TimeSpan.FromMinutes(config.MaxAgeMinutes);
        }

        return TimeSpan.Zero;
    }
}
