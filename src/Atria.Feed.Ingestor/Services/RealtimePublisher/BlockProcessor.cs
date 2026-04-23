using Atria.Common.Models.Options;
using Atria.Feed.Ingestor.ChainClients.Models;
using Atria.Feed.Ingestor.Config.Options;
using Atria.Pipeline.Options;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Numerics;
using BlockchainContracts = Atria.Contracts.Subjects.Blockchain.Blockchain;

namespace Atria.Feed.Ingestor.Services.RealtimePublisher;

public class BlockProcessor
{
    private readonly ILogger<BlockProcessor> _logger;
    private readonly BlockKvStore _blockStore;
    private readonly ChainStateStore _stateStore;
    private readonly NetworkOptions _chainOptions;
    private readonly IngestorOptions _ingestorOptions;
    private readonly BlockProviderOptions _blockProviderOptions;

    public BlockProcessor(
        ILogger<BlockProcessor> logger,
        IOptions<IngestorNetworkOptions> networksOptions,
        IOptions<IngestorOptions> ingestorOptions,
        IOptions<BlockProviderOptions> blockProviderOptions,
        BlockKvStore blockStore,
        ChainStateStore stateStore)
    {
        _logger = logger;
        _chainOptions = networksOptions.Value.NetworkOptions;
        _ingestorOptions = ingestorOptions.Value;
        _blockProviderOptions = blockProviderOptions.Value;
        _blockStore = blockStore;
        _stateStore = stateStore;
    }

    public async Task StoreBlockAsync(
        BigInteger blockNumber,
        BlockData blockData,
        bool isReorg,
        CancellationToken ct)
    {
        blockData.Block.Metadata.IsReorg = isReorg;
        blockData.Logs.Metadata.IsReorg = isReorg;

        if (blockData.Traces != null)
        {
            blockData.Traces.Metadata.IsReorg = isReorg;
        }

        await CleanupOldBlocksAsync(blockNumber, ct);

        await StoreBlockDataAsync(blockNumber, blockData, ct);
        await _stateStore.PutBlockHashAsync(_chainOptions.Id, blockNumber, blockData.Block.Block.Hash, ct);
        await _stateStore.UpdateHeadAsync(_chainOptions.Id, blockNumber, ct);

        _logger.LogDebug(
            "Stored block {BlockNumber} (hash: {Hash}, reorg: {IsReorg})",
            blockNumber,
            blockData.Block.Block.Hash[..10],
            isReorg);
    }

    public async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        string operationName,
        CancellationToken ct)
    {
        var retryAttempts = _ingestorOptions.RetryAttempts;
        var retryDelay = TimeSpan.FromSeconds(_ingestorOptions.RetryDelaySeconds);

        for (var attempt = 1; attempt <= retryAttempts; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (attempt < retryAttempts)
            {
                var delay = retryDelay * Math.Pow(2, attempt - 1);
                _logger.LogWarning(
                    ex,
                    "Attempt {Attempt}/{MaxAttempts} failed for {Operation} on {Chain}. Retrying in {Delay}s",
                    attempt,
                    retryAttempts,
                    operationName,
                    _chainOptions.Id,
                    delay.TotalSeconds);

                await Task.Delay(delay, ct);
            }
        }
    }

    private async Task StoreBlockDataAsync(BigInteger blockNumber, BlockData blockData, CancellationToken ct)
    {
        var chain = _chainOptions.Id;

        await _blockStore.PutBlockAsync(
            chain,
            BlockchainContracts.DataTypes.Transactions,
            blockNumber,
            blockData.Block,
            ct);

        await _blockStore.PutBlockAsync(
            chain,
            BlockchainContracts.DataTypes.Logs,
            blockNumber,
            blockData.Logs,
            ct);

        if (blockData.Traces != null)
        {
            await _blockStore.PutBlockAsync(
                chain,
                BlockchainContracts.DataTypes.Traces,
                blockNumber,
                blockData.Traces,
                ct);
        }
    }

    private async Task CleanupOldBlocksAsync(BigInteger newHead, CancellationToken ct)
    {
        if (_blockProviderOptions.MaxBlocksCount is not { } maxBlocks)
        {
            return;
        }

        var chainId = _chainOptions.Id;
        var tail = await _stateStore.GetTailAsync(chainId, ct);

        if (tail is null)
        {
            tail = newHead - maxBlocks + 1;
            if (tail < 0)
            {
                tail = 0;
            }

            await _stateStore.UpdateTailAsync(chainId, tail.Value, ct);
            _logger.LogInformation(
                "Initialized tail for chain {ChainId} to {Tail}",
                chainId,
                tail);
            return;
        }

        var currentTail = tail.Value;
        while (newHead - currentTail >= maxBlocks)
        {
            await _blockStore.DeleteBlockAsync(chainId, currentTail, ct);
            await _stateStore.DeleteBlockHashAsync(chainId, currentTail, ct);
            currentTail++;
        }

        await _stateStore.UpdateTailAsync(chainId, currentTail, ct);
    }
}
