using Atria.Common.Messaging.Core;
using Atria.Pipeline.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.KeyValueStore;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Atria.Pipeline.Stores;

public sealed class ChainStateStore
{
    private readonly NatsConnectionManager _connectionManager;
    private readonly BlockProviderOptions _options;
    private readonly ILogger<ChainStateStore> _logger;

    public ChainStateStore(
        NatsConnectionManager connectionManager,
        IOptions<BlockProviderOptions> options,
        ILogger<ChainStateStore> logger)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BigInteger?> GetHeadAsync(string chainId, CancellationToken ct)
    {
        try
        {
            var store = await GetStoreAsync(ct);
            var key = GetHeadKey(chainId);

            var entry = await store.GetEntryAsync<string>(key, cancellationToken: ct);

            if (entry.Value is null)
            {
                return null;
            }

            return BigInteger.Parse(entry.Value);
        }
        catch (NatsKVKeyNotFoundException)
        {
            return null;
        }
        catch (NatsKVKeyDeletedException)
        {
            return null;
        }
    }

    public async Task UpdateHeadAsync(string chainId, BigInteger blockNumber, CancellationToken ct)
    {
        var store = await GetStoreAsync(ct);
        var key = GetHeadKey(chainId);

        await store.PutAsync(key, blockNumber.ToString(), cancellationToken: ct);

        _logger.LogDebug("Updated head for chain {ChainId} to {BlockNumber}", chainId, blockNumber);
    }

    public async Task WaitForHeadAsync(string chainId, BigInteger targetBlock, CancellationToken ct)
    {
        var current = await GetHeadAsync(chainId, ct);
        if (current.HasValue && current.Value >= targetBlock)
        {
            return;
        }

        var store = await GetStoreAsync(ct);
        var key = GetHeadKey(chainId);

        await foreach (var entry in store.WatchAsync<string>(key, cancellationToken: ct))
        {
            if (entry.Value is not null && entry.Operation == NatsKVOperation.Put)
            {
                if (BigInteger.TryParse(entry.Value, out var head) && head >= targetBlock)
                {
                    return;
                }
            }
        }
    }

    public async IAsyncEnumerable<BigInteger> StreamForHeadAsync(string chainId, [EnumeratorCancellation] CancellationToken ct)
    {
        var store = await GetStoreAsync(ct);
        var key = GetHeadKey(chainId);

        await foreach (var entry in store.WatchAsync<string>(key, cancellationToken: ct))
        {
            if (entry.Value is not null && entry.Operation == NatsKVOperation.Put)
            {
                if (BigInteger.TryParse(entry.Value, out var head))
                {
                    yield return head;
                }
            }
        }
    }

    public async Task<string?> GetBlockHashAsync(string chainId, BigInteger blockNumber, CancellationToken ct)
    {
        try
        {
            var store = await GetStoreAsync(ct);
            var key = GetHashKey(chainId, blockNumber);

            var entry = await store.GetEntryAsync<string>(key, cancellationToken: ct);

            return entry.Value;
        }
        catch (NatsKVKeyNotFoundException)
        {
            return null;
        }
        catch (NatsKVKeyDeletedException)
        {
            return null;
        }
    }

    public async Task PutBlockHashAsync(
        string chainId,
        BigInteger blockNumber,
        string hash,
        CancellationToken ct)
    {
        var store = await GetStoreAsync(ct);
        var key = GetHashKey(chainId, blockNumber);

        await store.PutAsync(key, hash, cancellationToken: ct);

        _logger.LogTrace(
            "Stored hash for block {BlockNumber} on chain {ChainId}: {Hash}",
            blockNumber,
            chainId,
            hash[..8]);
    }

    public async Task<BigInteger?> GetTailAsync(string chainId, CancellationToken ct)
    {
        try
        {
            var store = await GetStoreAsync(ct);
            var key = GetTailKey(chainId);

            var entry = await store.GetEntryAsync<string>(key, cancellationToken: ct);

            if (entry.Value is null)
            {
                return null;
            }

            return BigInteger.Parse(entry.Value);
        }
        catch (NatsKVKeyNotFoundException)
        {
            return null;
        }
        catch (NatsKVKeyDeletedException)
        {
            return null;
        }
    }

    public async Task UpdateTailAsync(string chainId, BigInteger blockNumber, CancellationToken ct)
    {
        var store = await GetStoreAsync(ct);
        var key = GetTailKey(chainId);

        await store.PutAsync(key, blockNumber.ToString(), cancellationToken: ct);

        _logger.LogDebug("Updated tail for chain {ChainId} to {BlockNumber}", chainId, blockNumber);
    }

    public async Task DeleteBlockHashAsync(string chainId, BigInteger blockNumber, CancellationToken ct)
    {
        try
        {
            var store = await GetStoreAsync(ct);
            var key = GetHashKey(chainId, blockNumber);

            await store.DeleteAsync(key, cancellationToken: ct);
        }
        catch (NatsKVKeyNotFoundException)
        {
            // Already deleted or never existed
        }
    }

    private static string GetHeadKey(string chainId) => $"{chainId}.head";

    private static string GetTailKey(string chainId) => $"{chainId}.tail";

    private static string GetHashKey(string chainId, BigInteger blockNumber) => $"{chainId}.hash.{blockNumber}";

    private ValueTask<INatsKVStore> GetStoreAsync(CancellationToken ct)
        => _connectionManager.GetKVStoreAsync(_options.ChainStateBucket, ct);
}
