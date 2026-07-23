using Atria.Common.Helpers.Json;
using Atria.Common.Messaging.ServiceBus;
using Atria.Common.Observability.Models;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;
using Atria.Contracts.Subjects.Feed;
using Atria.Feed.Runtime.Engine;
using Atria.Feed.Runtime.Engine.Exceptions;
using Atria.Feed.Runtime.Engine.Models;
using Atria.Feed.Runtime.Observability;
using Atria.Pipeline.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;

namespace Atria.Feed.Runtime.Processing;

public sealed class FeedBlockProcessor
{
    private const int MaxRetries = 3;
    private const int MaxBlockErrors = 50;
    private const int MaxProcessingErrors = 5;

    private readonly IBlockProvider _blockProvider;
    private readonly IFeedCursorStore _cursorStore;
    private readonly IFeedPublisher _feedPublisher;
    private readonly IServiceBus _serviceBus;
    private readonly FeedManager _feedManager;
    private readonly RuntimeMetricsRecorder _metrics;
    private readonly ILogger<FeedBlockProcessor> _logger;

    public FeedBlockProcessor(
        IBlockProvider blockProvider,
        IFeedCursorStore cursorStore,
        IFeedPublisher feedPublisher,
        IServiceBus serviceBus,
        FeedManager feedManager,
        RuntimeMetricsRecorder metrics,
        ILogger<FeedBlockProcessor> logger)
    {
        _blockProvider = blockProvider;
        _cursorStore = cursorStore;
        _feedPublisher = feedPublisher;
        _serviceBus = serviceBus;
        _feedManager = feedManager;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task ProcessAsync(FeedRuntimeContext feed, CancellationToken ct)
    {
        var feedId = feed.FeedRuntime.Id;
        var chainId = feed.FeedRuntime.ChainId;
        var dataType = feed.FeedRuntime.DataType.ToString().ToLowerInvariant();
        var blockDelay = feed.FeedRuntime.BlockDelay;
        var metricScope = BusinessMetricScope.Create(feed.FeedRuntime.ResourceNamespace, feedId);

        var cursor = await _cursorStore.GetAsync(feedId, ct);

        if (cursor is null)
        {
            cursor = feed.FeedRuntime.StartBlock ?? await _blockProvider.GetHeadAsync(chainId, ct);
            _logger.LogInformation("Feed {FeedId} starting from block {StartBlock}", feedId, cursor);
        }
        else
        {
            _logger.LogInformation("Feed {FeedId} resuming from cursor {Cursor}", feedId, cursor);
        }

        var consecutiveBlockErrors = 0;
        var consecutiveProcessingErrors = 0;

        await foreach (var block in _blockProvider.StreamBlocksAsync(chainId, dataType, cursor.Value, blockDelay, ct))
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogInformation("Feed cancellation requested ({FeedId})", feedId);
                break;
            }

            if (block.Data is null)
            {
                if (feed.FeedRuntime.ErrorHandling == ErrorHandlingStrategy.ContinueOnError)
                {
                    _logger.LogWarning(
                        "Feed {FeedId}: Block {Block} data not found, skipping (ContinueOnError)",
                        feedId,
                        block.BlockNumber);
                    await _cursorStore.SetAsync(feedId, block.BlockNumber + 1, ct);
                    consecutiveBlockErrors = 0;
                    continue;
                }

                consecutiveBlockErrors++;
                _logger.LogWarning(
                    "Feed {FeedId}: Block {Block} data not found ({ErrorCount}/{MaxErrors} consecutive errors)",
                    feedId,
                    block.BlockNumber,
                    consecutiveBlockErrors,
                    MaxBlockErrors);

                if (consecutiveBlockErrors >= MaxBlockErrors)
                {
                    _metrics.RecordMissingDataFailure(metricScope);
                    _logger.LogError(
                        "Feed {FeedId}: Max consecutive block errors reached ({MaxErrors}), pausing",
                        feedId,
                        MaxBlockErrors);

                    await _cursorStore.SetAsync(feedId, block.BlockNumber + 1, ct);

                    await PublishPauseEventAsync(
                        feedId,
                        feed.FeedRuntime.DeployId,
                        FeedPauseSource.BlockErrors,
                        $"Block {block.BlockNumber}: data was unavailable for {MaxBlockErrors} consecutive blocks.",
                        ct);
                    return;
                }

                await _cursorStore.SetAsync(feedId, block.BlockNumber + 1, ct);
                continue;
            }

            consecutiveBlockErrors = 0;

            var success = await ProcessBlockWithRetriesAsync(
                feed,
                block.Data,
                block.DataSizeBytes,
                block.BlockNumber,
                ct);

            if (!success)
            {
                if (feed.FeedRuntime.ErrorHandling == ErrorHandlingStrategy.ContinueOnError)
                {
                    consecutiveProcessingErrors = 0;
                }
                else
                {
                    consecutiveProcessingErrors++;

                    if (consecutiveProcessingErrors >= MaxProcessingErrors)
                    {
                        _logger.LogError(
                            "Feed {FeedId}: Max consecutive processing errors reached ({MaxErrors}), pausing",
                            feedId,
                            MaxProcessingErrors);
                        await _cursorStore.SetAsync(feedId, block.BlockNumber + 1, ct);
                        await PublishPauseEventAsync(
                            feedId,
                            feed.FeedRuntime.DeployId,
                            FeedPauseSource.ProcessingErrors,
                            $"Block {block.BlockNumber}: processing failed for {MaxProcessingErrors} consecutive blocks.",
                            ct);
                        return;
                    }
                }

                await _cursorStore.SetAsync(feedId, block.BlockNumber + 1, ct);
            }
            else
            {
                consecutiveProcessingErrors = 0;
                await _cursorStore.SetAsync(feedId, block.BlockNumber + 1, ct);
            }
        }
    }

    private async Task<bool> ProcessBlockWithRetriesAsync(
        FeedRuntimeContext feed,
        object blockData,
        int? inputSizeBytes,
        BigInteger blockNumber,
        CancellationToken ct)
    {
        var feedId = feed.FeedRuntime.Id;
        var retryCount = 0;
        var startedAt = Stopwatch.GetTimestamp();
        var scope = BusinessMetricScope.Create(feed.FeedRuntime.ResourceNamespace, feedId);

        while (retryCount < MaxRetries)
        {
            try
            {
                await ProcessBlockDataAsync(feed, blockData, blockNumber, scope, ct);
                _metrics.RecordBlockCompleted(scope, inputSizeBytes, Stopwatch.GetElapsedTime(startedAt));
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (retryCount < MaxRetries - 1)
            {
                retryCount++;
                _metrics.RecordRetry();
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                _logger.LogWarning(
                    ex,
                    "Feed {FeedId} failed at block {Block}, retry {Retry}/{Max} in {Delay}s",
                    feedId,
                    blockNumber,
                    retryCount,
                    MaxRetries,
                    delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                _metrics.RecordTerminalFailure(scope, ex, Stopwatch.GetElapsedTime(startedAt));
                if (feed.FeedRuntime.ErrorHandling == ErrorHandlingStrategy.ContinueOnError)
                {
                    _logger.LogError(
                        ex,
                        "Feed {FeedId} failed at block {Block} after {Max} retries, skipping (ContinueOnError)",
                        feedId,
                        blockNumber,
                        MaxRetries);
                }
                else
                {
                    _logger.LogError(
                        ex,
                        "Feed {FeedId} failed at block {Block} after {Max} retries",
                        feedId,
                        blockNumber,
                        MaxRetries);
                }

                return false;
            }
        }

        return false;
    }

    private async Task ProcessBlockDataAsync(
        FeedRuntimeContext feed,
        object blockData,
        BigInteger blockNumber,
        BusinessMetricScope? scope,
        CancellationToken ct)
    {
        var feedId = feed.FeedRuntime.Id;

        _logger.LogDebug("Feed {FeedId} processing block {BlockNumber}", feedId, blockNumber);

        var dataToSend = await _feedManager.ExecuteAsync(feedId, blockData, ct: ct);
        ct.ThrowIfCancellationRequested();

        if (dataToSend != null)
        {
            var outputSizeBytes = JsonSerializer.SerializeToUtf8Bytes(
                dataToSend,
                AtriaJsonSerializerOptions.Create(JsonSerializerOptions.Default)).Length;
            var output = new FeedOutputData(
                feedId,
                feed.FeedRuntime.OutputIds,
                dataToSend,
                IsTestExecution: false,
                BlockNumber: blockNumber.ToString(),
                DeployId: feed.FeedRuntime.DeployId,
                ResourceNamespace: feed.FeedRuntime.ResourceNamespace,
                DataSizeBytes: outputSizeBytes);

            try
            {
                await _feedPublisher.PublishResultAsync(feedId, output, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _metrics.RecordOutputPublishFailure();
                throw new OutputPublishException(ex);
            }

            _metrics.RecordOutputPublished(scope, outputSizeBytes);
            _logger.LogTrace("Output published for feed {FeedId}", feedId);
        }
    }

    private async Task PublishPauseEventAsync(
        string feedId,
        string deployId,
        FeedPauseSource source,
        string? reason,
        CancellationToken ct)
    {
        try
        {
            var pauseEvent = new FeedPausedEvent(feedId, source, reason, deployId);
            await _serviceBus.PublishAsync(FeedSubjects.System.FeedPaused, pauseEvent, ct);
            _logger.LogInformation("Published pause event for feed {FeedId} (source: {Source})", feedId, source);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish pause event for feed {FeedId}", feedId);
        }
    }
}
