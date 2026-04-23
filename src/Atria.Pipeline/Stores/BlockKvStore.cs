using Atria.Common.Messaging.Core;
using Atria.Contracts.Events.Feed.Enums;
using Atria.Pipeline.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.KeyValueStore;
using System.Numerics;
using System.Text.Json;
using BlockchainContracts = Atria.Contracts.Subjects.Blockchain.Blockchain;

namespace Atria.Pipeline.Stores;

public sealed class BlockKvStore
{
    private static readonly string[] DataTypes = Enum.GetValues<FeedDataType>()
        .Select(BlockchainContracts.DataTypes.FromFeedDataType)
        .ToArray();

    private readonly NatsConnectionManager _connectionManager;
    private readonly BlockProviderOptions _options;
    private readonly MessagingKVConfig _defaultBlocksConfig;
    private readonly ILogger<BlockKvStore> _logger;

    public BlockKvStore(
        NatsConnectionManager connectionManager,
        IOptions<BlockProviderOptions> options,
        ILogger<BlockKvStore> logger)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
        _logger = logger;

        _defaultBlocksConfig = new MessagingKVConfig
        {
            MaxAgeMinutes = _options.DefaultBlocksMaxAgeMinutes,
            MaxSizeMb = _options.DefaultBlocksMaxSizeMb,
            History = 1,
            Replicas = 1,
            Compression = _options.DefaultBlocksCompression,
        };
    }

    public async Task PutBlockAsync<T>(
        string chainId,
        string dataType,
        BigInteger blockNumber,
        T data,
        CancellationToken ct)
    {
        var store = await GetStoreAsync(chainId, ct);
        var key = FormatKey(dataType, blockNumber);
        var json = JsonSerializer.SerializeToUtf8Bytes(data);

        await store.PutAsync(key, json, cancellationToken: ct);

        _logger.LogTrace(
            "Stored block {BlockNumber} ({DataType}) for chain {ChainId}",
            blockNumber,
            dataType,
            chainId);
    }

    public async Task<T?> GetBlockAsync<T>(
        string chainId,
        string dataType,
        BigInteger blockNumber,
        CancellationToken ct)
        where T : class
    {
        try
        {
            var store = await GetStoreAsync(chainId, ct);
            var key = FormatKey(dataType, blockNumber);

            var entry = await store.GetEntryAsync<byte[]>(key, cancellationToken: ct);

            if (entry.Value is null or { Length: 0 })
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(entry.Value);
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

    public async Task DeleteBlockAsync(string chainId, BigInteger blockNumber, CancellationToken ct)
    {
        var store = await GetStoreAsync(chainId, ct);

        foreach (var dataType in DataTypes)
        {
            var key = FormatKey(dataType, blockNumber);
            try
            {
                await store.DeleteAsync(key, cancellationToken: ct);
            }
            catch (NatsKVKeyNotFoundException)
            {
                // Block may not have traces — ignore
            }
        }

        _logger.LogTrace(
            "Deleted block {BlockNumber} for chain {ChainId}",
            blockNumber,
            chainId);
    }

    private static string FormatKey(string dataType, BigInteger blockNumber)
        => $"{dataType}.{blockNumber}";

    private ValueTask<INatsKVStore> GetStoreAsync(string chainId, CancellationToken ct)
    {
        var bucketName = $"{_options.BlocksBucketPrefix}-{chainId}";
        return _connectionManager.GetKVStoreAsync(bucketName, _defaultBlocksConfig, ct);
    }
}
