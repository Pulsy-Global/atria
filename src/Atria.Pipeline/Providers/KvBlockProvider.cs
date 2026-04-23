using Atria.Pipeline.Interfaces;
using Atria.Pipeline.Models;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Atria.Pipeline.Providers;

public sealed class KvBlockProvider : IBlockProvider
{
    private readonly BlockKvStore _blockStore;
    private readonly ChainStateStore _stateStore;
    private readonly ILogger<KvBlockProvider> _logger;

    public KvBlockProvider(
        BlockKvStore blockStore,
        ChainStateStore stateStore,
        ILogger<KvBlockProvider> logger)
    {
        _blockStore = blockStore;
        _stateStore = stateStore;
        _logger = logger;
    }

    public async IAsyncEnumerable<BlockEnvelope> StreamBlocksAsync(
        string chainId,
        string dataType,
        BigInteger fromBlock,
        int blockDelay,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var cursor = fromBlock;

        _logger.LogDebug(
            "Starting block stream for chain {ChainId}, dataType {DataType}, from block {FromBlock}, delay {BlockDelay}",
            chainId,
            dataType,
            fromBlock,
            blockDelay);

        while (!ct.IsCancellationRequested)
        {
            await _stateStore.WaitForHeadAsync(chainId, cursor + blockDelay, ct);

            var data = await GetBlockAsync<object>(chainId, dataType, cursor, ct);
            var hash = await _stateStore.GetBlockHashAsync(chainId, cursor, ct);

            yield return new BlockEnvelope(cursor, hash, dataType, data);

            cursor++;
        }
    }

    public Task<T?> GetBlockAsync<T>(
        string chainId,
        string dataType,
        BigInteger blockNumber,
        CancellationToken ct)
        where T : class
    {
        return _blockStore.GetBlockAsync<T>(chainId, dataType, blockNumber, ct);
    }

    public async Task<BigInteger> GetHeadAsync(string chainId, CancellationToken ct)
    {
        var head = await _stateStore.GetHeadAsync(chainId, ct);
        return head ?? BigInteger.Zero;
    }
}
