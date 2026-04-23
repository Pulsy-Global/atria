using Atria.Common.Messaging.ServiceBus;
using Atria.Common.Models.Options;
using Atria.Contracts.Events.Blockchain;
using Atria.Contracts.Subjects.Blockchain;
using Atria.Feed.Ingestor.ChainClients;
using Atria.Feed.Ingestor.ChainClients.Interfaces;
using Atria.Feed.Ingestor.Config.Options;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Numerics;
using System.Threading.Channels;

namespace Atria.Feed.Ingestor.Services.RealtimePublisher;

public class RealtimePublisher : BackgroundService
{
    private readonly Channel<bool> _newBlockSignal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
    private readonly ILogger<RealtimePublisher> _logger;
    private readonly IServiceBus _serviceBus;
    private readonly EvmClient _evmClient;
    private readonly IEvmWebSocketClient _wsClient;
    private readonly ChainStateStore _stateStore;
    private readonly NetworkOptions _chainOptions;
    private readonly IngestorOptions _ingestorOptions;
    private readonly BlockProcessor _blockProcessor;
    private readonly ServiceStateManager _stateManager;

    public RealtimePublisher(
        ILogger<RealtimePublisher> logger,
        IServiceBus serviceBus,
        IClientFactory<EvmClient> evmClientFactory,
        IEvmWebSocketClient wsClient,
        IOptions<IngestorOptions> ingestorOptions,
        IOptions<IngestorNetworkOptions> networksOptions,
        ChainStateStore stateStore,
        BlockProcessor blockProcessor,
        ServiceStateManager stateManager)
    {
        _logger = logger;
        _serviceBus = serviceBus;
        _wsClient = wsClient;
        _chainOptions = networksOptions.Value.NetworkOptions;
        _ingestorOptions = ingestorOptions.Value;
        _stateStore = stateStore;
        _blockProcessor = blockProcessor;
        _stateManager = stateManager;
        _evmClient = evmClientFactory.CreateClient();
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_ingestorOptions.EnableRealtimeIngestion)
        {
            _logger.LogInformation("Realtime ingestion is disabled for {Chain}", _chainOptions.Id);
            return;
        }

        if (string.IsNullOrEmpty(_chainOptions.NodeWsUrl))
        {
            _logger.LogWarning("No WebSocket URL configured for {Chain}, using polling only", _chainOptions.Id);
            await RunProcessingLoopAsync(ct);
        }
        else
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var processingTask = RunProcessingLoopAsync(cts.Token);
            var wsTask = RunWebSocketLoopAsync(cts.Token);

            await Task.WhenAny(processingTask, wsTask);
            await cts.CancelAsync();

            await Task.WhenAll(processingTask, wsTask);
        }
    }

    private async Task RunProcessingLoopAsync(CancellationToken ct)
    {
        var state = await _stateManager.LoadCurrentStateAsync(ct);
        var lastProcessed = state.LastProcessedBlock;

        _logger.LogInformation(
            "Starting realtime processing for {Chain} from block {Block}",
            _chainOptions.Id,
            lastProcessed + 1);

        while (!ct.IsCancellationRequested)
        {
            lastProcessed = await ProcessPendingBlocksAsync(lastProcessed, ct);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_ingestorOptions.BlockPollIntervalSec));

            try
            {
                await _newBlockSignal.Reader.ReadAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("No WS signal, polling for {Chain}", _chainOptions.Id);
            }
        }
    }

    private async Task RunWebSocketLoopAsync(CancellationToken ct)
    {
        var inactivityTimeout = TimeSpan.FromSeconds(_ingestorOptions.WsInactivityTimeoutSec);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _wsClient.ListenAsync(_newBlockSignal.Writer, inactivityTimeout, ct);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("WS inactivity timeout for {Chain}, reconnecting...", _chainOptions.Id);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "WS disconnected for {Chain}, reconnecting...", _chainOptions.Id);

                await Task.Delay(TimeSpan.FromSeconds(_ingestorOptions.WsReconnectDelaySec), ct);
            }
        }
    }

    private async Task<BigInteger> ProcessPendingBlocksAsync(BigInteger lastProcessed, CancellationToken ct)
    {
        var chainHead = await _evmClient.GetLatestBlockNumberAsync(ct);
        IDisposable? reorgScope = null;

        try
        {
            while (lastProcessed < chainHead && !ct.IsCancellationRequested)
            {
                var gap = (int)(chainHead - lastProcessed);
                var batchSize = Math.Min(gap, _ingestorOptions.CatchUpBatchSize);

                if (batchSize <= 1)
                {
                    var blockData = await _evmClient.GetByBlockNumberAsync(lastProcessed + 1, ct);
                    var result = await ProcessFetchedBlockAsync(lastProcessed + 1, lastProcessed, blockData, ct);

                    if (result.RequiresRewind)
                    {
                        lastProcessed = result.RewindTo;
                        reorgScope ??= ReorgContext.Activate();
                        continue;
                    }

                    lastProcessed++;
                }
                else
                {
                    var fetchTasks = Enumerable.Range(1, batchSize)
                        .Select(i => _evmClient.GetByBlockNumberAsync(lastProcessed + i, ct))
                        .ToList();

                    var blocks = await Task.WhenAll(fetchTasks);
                    var batchStart = lastProcessed;

                    for (var i = 0; i < blocks.Length; i++)
                    {
                        var blockNumber = batchStart + i + 1;
                        var prevBlock = batchStart + i;
                        var result = await ProcessFetchedBlockAsync(blockNumber, prevBlock, blocks[i], ct);

                        if (result.RequiresRewind)
                        {
                            lastProcessed = result.RewindTo;
                            reorgScope ??= ReorgContext.Activate();
                            break;
                        }

                        lastProcessed = blockNumber;
                    }
                }
            }
        }
        finally
        {
            reorgScope?.Dispose();
        }

        return lastProcessed;
    }

    private async Task<(bool RequiresRewind, BigInteger RewindTo)> ProcessFetchedBlockAsync(
        BigInteger nextBlock,
        BigInteger lastProcessed,
        ChainClients.Models.BlockData blockData,
        CancellationToken ct)
    {
        var currentHash = blockData.Block.Block.Hash;
        var parentHash = blockData.Block.Block.ParentHash;

        if (nextBlock > 0)
        {
            var reorgResult = await CheckForReorgAsync(nextBlock, parentHash, lastProcessed, ct);
            if (reorgResult.RequiresRewind)
            {
                return reorgResult;
            }
        }

        var savedHash = await _stateStore.GetBlockHashAsync(_chainOptions.Id, nextBlock, ct);

        if (savedHash == null)
        {
            await ProcessBlockAsync(blockData, nextBlock, isReorg: false, ct);
        }
        else if (currentHash == savedHash)
        {
            await _stateStore.UpdateHeadAsync(_chainOptions.Id, nextBlock, ct);
            _logger.LogDebug("Block {Block} already processed, updated head", nextBlock);
        }
        else
        {
            await ProcessBlockAsync(blockData, nextBlock, isReorg: true, ct);
        }

        return (false, BigInteger.Zero);
    }

    private async Task<(bool RequiresRewind, BigInteger RewindTo)> CheckForReorgAsync(
        BigInteger nextBlock,
        string parentHash,
        BigInteger lastProcessed,
        CancellationToken ct)
    {
        var savedParentHash = await _stateStore.GetBlockHashAsync(
            _chainOptions.Id,
            nextBlock - 1,
            ct);

        if (savedParentHash == null || parentHash == savedParentHash)
        {
            return (false, BigInteger.Zero);
        }

        try
        {
            var rewindTo = await RewindToCommonAncestorAsync(lastProcessed, ct);

            var reorgEvent = new ReorgEvent(
                ChainId: _chainOptions.Id,
                FromBlock: (ulong)lastProcessed,
                ToBlock: (ulong)rewindTo);

            await _serviceBus.PublishAsync(
                Blockchain.Subjects.ReorgDetected(_chainOptions.Id),
                reorgEvent,
                ct);

            return (true, rewindTo);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogCritical(
                ex,
                "Deep reorg detected for {Chain}, stopping realtime processing",
                _chainOptions.Id);
            throw;
        }
    }

    private async Task<BigInteger> RewindToCommonAncestorAsync(BigInteger from, CancellationToken ct)
    {
        using var rewindScope = ReorgContext.Activate();

        for (var i = from; i > from - _ingestorOptions.MaxReorgDepth && i >= 0; i--)
        {
            var block = await _evmClient.GetByBlockNumberAsync(i, ct);
            var savedHash = await _stateStore.GetBlockHashAsync(_chainOptions.Id, i, ct);

            if (savedHash != null && block.Block.Block.Hash == savedHash)
            {
                _logger.LogWarning(
                    "Reorg detected for {Chain}, rewinding from {From} to ancestor {Ancestor}",
                    _chainOptions.Id,
                    from,
                    i);
                return i;
            }
        }

        throw new InvalidOperationException(
            $"Reorg depth exceeds maximum of {_ingestorOptions.MaxReorgDepth} for {_chainOptions.Id}");
    }

    private async Task ProcessBlockAsync(
        ChainClients.Models.BlockData blockData,
        BigInteger blockNumber,
        bool isReorg,
        CancellationToken ct)
    {
        try
        {
            await _blockProcessor.ExecuteWithRetryAsync(
                () => _blockProcessor.StoreBlockAsync(blockNumber, blockData, isReorg, ct),
                $"Process block {blockNumber}",
                ct);

            _logger.LogInformation(
                "Processed block {BlockNumber} for {Chain} | Reorg: {IsReorg}",
                blockNumber,
                _chainOptions.Id,
                isReorg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process block {BlockNumber} for {Chain}", blockNumber, _chainOptions.Id);
            throw;
        }
    }
}
