using Atria.Common.Messaging.ServiceBus;
using Atria.Contracts.Events.Blockchain;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;
using Atria.Contracts.Subjects.Blockchain;
using Atria.Contracts.Subjects.Feed;
using Atria.Feed.Runtime.Engine.Models;
using Atria.Pipeline.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace Atria.Feed.Runtime.Services;

public sealed class FeedRuntimeService : BackgroundService
{
    private readonly FeedLifecycleManager _lifecycle;
    private readonly IFeedCursorStore _cursorStore;
    private readonly IServiceBus _serviceBus;
    private readonly ILogger<FeedRuntimeService> _logger;

    public FeedRuntimeService(
        FeedLifecycleManager lifecycle,
        IFeedCursorStore cursorStore,
        IServiceBus serviceBus,
        ILogger<FeedRuntimeService> logger)
    {
        _lifecycle = lifecycle;
        _cursorStore = cursorStore;
        _serviceBus = serviceBus;
        _logger = logger;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Feed Runtime Service, instance {InstanceId}", _lifecycle.InstanceId);
        await _lifecycle.StopAllAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting Feed Runtime Service, instance {InstanceId}", _lifecycle.InstanceId);

        await Task.WhenAll(
            HandleDeployRequests(ct),
            HandlePauseRequests(ct),
            HandleDeleteRequests(ct),
            HandleReorgEvents(ct),
            RunLeaseRenewalLoopAsync(ct));
    }

    private async Task HandleDeployRequests(CancellationToken ct)
    {
        await foreach (var request in _serviceBus.SubscribeAsync<FeedDeployRequest>(
            FeedSubjects.System.DeployRequest,
            queueGroup: "deploy-workers",
            ct: ct))
        {
            try
            {
                var feedLock = _lifecycle.GetFeedLock(request.Id);
                await feedLock.WaitAsync(ct);
                try
                {
                    var feedRuntime = new FeedRuntime
                    {
                        Id = request.Id,
                        ChainId = request.ChainId,
                        DataType = request.FeedDataType,
                        FilterLangKind = request.FilterLangKind,
                        FunctionLangKind = request.FunctionLangKind,
                        FilterCode = request.FilterCode,
                        FunctionCode = request.FunctionCode,
                        Type = request.Type,
                        OutputIds = request.OutputIds,
                        BlockDelay = request.BlockDelay,
                        StartBlock = request.StartBlock.HasValue ? new BigInteger(request.StartBlock.Value) : null,
                        ErrorHandling = request.ErrorHandling,
                        EkvNamespace = request.EkvNamespace,
                    };

                    var started = await _lifecycle.TryStartWithLeaseAsync(feedRuntime, ct);
                    if (started)
                    {
                        await PublishDeployedEventAsync(request.Id, ct);
                    }
                }
                finally
                {
                    feedLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feed deploy error ({FeedId})", request.Id);
            }
        }
    }

    private async Task HandlePauseRequests(CancellationToken ct)
    {
        await foreach (var request in _serviceBus.SubscribeAsync<FeedPauseRequest>(
            FeedSubjects.System.PauseRequest,
            ct: ct))
        {
            try
            {
                var feedLock = _lifecycle.GetFeedLock(request.Id);
                await feedLock.WaitAsync(ct);
                try
                {
                    if (!_lifecycle.RunningFeedIds.Contains(request.Id))
                    {
                        continue;
                    }

                    await _lifecycle.StopAsync(request.Id, deleteCursor: false, ct);
                    await PublishPauseEventAsync(request.Id, request.Source, ct);
                }
                finally
                {
                    feedLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feed pause error ({FeedId})", request.Id);
            }
        }
    }

    private async Task HandleDeleteRequests(CancellationToken ct)
    {
        await foreach (var request in _serviceBus.SubscribeAsync<FeedDeleteRequest>(
            FeedSubjects.System.DeleteRequest,
            ct: ct))
        {
            if (request == null)
            {
                continue;
            }

            try
            {
                var feedLock = _lifecycle.GetFeedLock(request.Id);
                await feedLock.WaitAsync(ct);
                try
                {
                    if (!_lifecycle.RunningFeedIds.Contains(request.Id))
                    {
                        continue;
                    }

                    await _lifecycle.StopAsync(request.Id, deleteCursor: true, ct);
                }
                finally
                {
                    feedLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Feed delete error ({FeedId})", request.Id);
            }
        }
    }

    private async Task HandleReorgEvents(CancellationToken ct)
    {
        await foreach (var reorg in _serviceBus.SubscribeAsync<ReorgEvent>(
            Blockchain.Subjects.ReorgDetectedAll,
            queueGroup: null,
            ct: ct))
        {
            try
            {
                _logger.LogWarning(
                    "Received reorg event for chain {ChainId}: blocks {FromBlock} -> {ToBlock}",
                    reorg.ChainId,
                    reorg.FromBlock,
                    reorg.ToBlock);

                var affectedFeeds = _lifecycle.RunningFeedIds
                    .Select(id => _lifecycle.GetFeed(id))
                    .Where(f => f != null && f.FeedRuntime.ChainId == reorg.ChainId)
                    .ToList();

                await Task.WhenAll(
                    affectedFeeds.Select(feed => RollbackFeedCursorIfNeededAsync(feed!, reorg, ct)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling reorg event for chain {ChainId}", reorg.ChainId);
            }
        }
    }

    private async Task RollbackFeedCursorIfNeededAsync(
        FeedRuntimeContext feed,
        ReorgEvent reorg,
        CancellationToken ct)
    {
        var feedId = feed.FeedRuntime.Id;
        var feedLock = _lifecycle.GetFeedLock(feedId);

        await feedLock.WaitAsync(ct);
        try
        {
            var cursor = await _cursorStore.GetAsync(feedId, ct);
            if (cursor == null)
            {
                return;
            }

            var lastProcessedBlock = (ulong)cursor.Value - 1;
            if (lastProcessedBlock > reorg.ToBlock)
            {
                var newCursor = new BigInteger(reorg.ToBlock + 1);

                await _lifecycle.StopProcessingAsync(feedId, deleteCursor: false);
                await _cursorStore.SetAsync(feedId, newCursor, ct);
                _lifecycle.StartProcessing(feedId, ct);

                _logger.LogWarning(
                    "Feed {FeedId} restarted with cursor {NewCursor} due to reorg (was: {OldCursor})",
                    feedId,
                    newCursor,
                    cursor.Value);
            }
        }
        finally
        {
            feedLock.Release();
        }
    }

    private async Task RunLeaseRenewalLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_lifecycle.RenewalInterval, ct);

                var lostLeases = await _lifecycle.RenewLeasesAsync(ct);

                foreach (var feedId in lostLeases)
                {
                    _logger.LogWarning("Lost lease for feed {FeedId}, stopping processing", feedId);

                    var feedLock = _lifecycle.GetFeedLock(feedId);
                    await feedLock.WaitAsync(ct);
                    try
                    {
                        await _lifecycle.StopProcessingAsync(feedId, deleteCursor: false);
                    }
                    finally
                    {
                        feedLock.Release();
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in lease renewal loop");
            }
        }
    }

    private async Task PublishPauseEventAsync(string feedId, FeedPauseSource source, CancellationToken ct)
    {
        try
        {
            await _serviceBus.PublishAsync(
                FeedSubjects.System.FeedPaused,
                new FeedPausedEvent(feedId, source),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish pause event for feed {FeedId}", feedId);
        }
    }

    private async Task PublishDeployedEventAsync(string feedId, CancellationToken ct)
    {
        try
        {
            await _serviceBus.PublishAsync(
                FeedSubjects.System.FeedDeployed,
                new FeedDeployedEvent(feedId),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish deployed event for feed {FeedId}", feedId);
        }
    }
}
