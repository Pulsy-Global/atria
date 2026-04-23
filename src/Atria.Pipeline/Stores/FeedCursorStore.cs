using Atria.Common.Messaging.Core;
using Atria.Pipeline.Interfaces;
using Atria.Pipeline.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.KeyValueStore;
using System.Numerics;

namespace Atria.Pipeline.Stores;

public sealed class FeedCursorStore : IFeedCursorStore
{
    private readonly NatsConnectionManager _connectionManager;
    private readonly BlockProviderOptions _options;
    private readonly ILogger<FeedCursorStore> _logger;

    public FeedCursorStore(
        NatsConnectionManager connectionManager,
        IOptions<BlockProviderOptions> options,
        ILogger<FeedCursorStore> logger)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BigInteger?> GetAsync(string feedId, CancellationToken ct)
    {
        try
        {
            var store = await GetStoreAsync(ct);
            var entry = await store.GetEntryAsync<string>(feedId, cancellationToken: ct);

            if (entry.Value is null)
            {
                _logger.LogWarning("Cursor for feed {FeedId} is null", feedId);
                return null;
            }

            _logger.LogDebug("Loaded cursor for feed {FeedId}: {Cursor}", feedId, entry.Value);
            return BigInteger.Parse(entry.Value);
        }
        catch (NatsKVKeyNotFoundException)
        {
            return null;
        }
        catch (NatsKVKeyDeletedException)
        {
            _logger.LogWarning("Cursor was deleted for feed {FeedId}", feedId);
            return null;
        }
    }

    public async Task SetAsync(string feedId, BigInteger cursor, CancellationToken ct)
    {
        var store = await GetStoreAsync(ct);
        await store.PutAsync(feedId, cursor.ToString(), cancellationToken: ct);
    }

    public async Task DeleteAsync(string feedId, CancellationToken ct)
    {
        try
        {
            var store = await GetStoreAsync(ct);
            await store.DeleteAsync(feedId, cancellationToken: ct);
        }
        catch (NatsKVKeyNotFoundException)
        {
            // Already deleted
        }
    }

    private ValueTask<INatsKVStore> GetStoreAsync(CancellationToken ct)
        => _connectionManager.GetKVStoreAsync(_options.CursorsBucket, ct);
}
