using Atria.Feed.Runtime.Engine;
using Atria.Feed.Runtime.Engine.Models;
using Atria.Feed.Runtime.Processing;
using Atria.Pipeline.Interfaces;
using Atria.Pipeline.Options;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Atria.Feed.Runtime.Services;

public sealed class FeedLifecycleManager
{
    private const string ResourceType = LeaseResources.FeedRuntime;

    private readonly FeedManager _feedManager;
    private readonly FeedBlockProcessor _blockProcessor;
    private readonly IFeedCursorStore _cursorStore;
    private readonly LeaseStore _leaseStore;
    private readonly LeaseOptions _leaseOptions;
    private readonly ILogger<FeedLifecycleManager> _logger;
    private readonly string _instanceId;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _feedSubscriptions = new();
    private readonly ConcurrentDictionary<string, Task> _feedTasks = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _feedLocks = new();

    public FeedLifecycleManager(
        FeedManager feedManager,
        FeedBlockProcessor blockProcessor,
        IFeedCursorStore cursorStore,
        LeaseStore leaseStore,
        IOptions<LeaseOptions> leaseOptions,
        ILogger<FeedLifecycleManager> logger)
    {
        _feedManager = feedManager;
        _blockProcessor = blockProcessor;
        _cursorStore = cursorStore;
        _leaseStore = leaseStore;
        _leaseOptions = leaseOptions.Value;
        _logger = logger;
        _instanceId = $"{Environment.MachineName}-{Guid.NewGuid():N}"[..32];
    }

    public string InstanceId => _instanceId;

    public IEnumerable<string> RunningFeedIds => _feedSubscriptions.Keys;

    public SemaphoreSlim GetFeedLock(string feedId)
        => _feedLocks.GetOrAdd(feedId, _ => new SemaphoreSlim(1, 1));

    public FeedRuntimeContext? GetFeed(string feedId)
        => _feedManager.Get(feedId);

    public async Task<bool> TryStartWithLeaseAsync(FeedRuntime feedRuntime, CancellationToken globalCt)
    {
        var feedId = feedRuntime.Id;

        if (_feedSubscriptions.ContainsKey(feedId))
        {
            _logger.LogDebug("Feed {FeedId} already running on this instance, skipping", feedId);
            return false;
        }

        var acquired = await _leaseStore.TryAcquireAsync(ResourceType, feedId, _instanceId, globalCt);

        if (!acquired)
        {
            _logger.LogDebug("Failed to acquire lease for feed {FeedId}, another instance owns it", feedId);
            return false;
        }

        await StopProcessingAsync(feedId, deleteCursor: false);
        await _feedManager.DeployAsync(feedRuntime);
        StartProcessing(feedId, globalCt);

        _logger.LogInformation("Feed started with lease: {FeedId}", feedId);
        return true;
    }

    public async Task StopAsync(string feedId, bool deleteCursor, CancellationToken ct)
    {
        await StopProcessingAsync(feedId, deleteCursor);
        await _leaseStore.ReleaseAsync(ResourceType, feedId, _instanceId, ct);
    }

    public async Task StopProcessingAsync(string feedId, bool deleteCursor)
    {
        if (_feedSubscriptions.TryGetValue(feedId, out var cts))
        {
            try
            {
                await cts.CancelAsync();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        if (_feedTasks.TryGetValue(feedId, out var feedTask))
        {
            try
            {
                await feedTask.WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
            }
            catch (TimeoutException)
            {
                _logger.LogError("Feed task {FeedId} did not complete within 30s", feedId);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _feedSubscriptions.TryRemove(feedId, out _);
        _feedTasks.TryRemove(feedId, out _);

        await _feedManager.StopAsync(feedId, CancellationToken.None);

        if (deleteCursor)
        {
            await _cursorStore.DeleteAsync(feedId, CancellationToken.None);
            _logger.LogDebug("Deleted cursor for {FeedId}", feedId);
        }

        _logger.LogDebug("Feed stopped: {FeedId}", feedId);
    }

    public void StartProcessing(string feedId, CancellationToken globalCt)
    {
        var feed = _feedManager.Get(feedId);
        if (feed == null)
        {
            _logger.LogWarning("Feed not found in registry on start: {FeedId}", feedId);
            return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(globalCt);

        if (!_feedSubscriptions.TryAdd(feedId, cts))
        {
            _logger.LogDebug("Feed {FeedId} is already running, skipping start", feedId);
            cts.Dispose();
            return;
        }

        _feedTasks[feedId] = Task.Run(
            async () =>
            {
                try
                {
                    await _blockProcessor.ProcessAsync(feed, cts.Token);
                }
                catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                {
                    _logger.LogDebug("Feed {FeedId} cancelled", feedId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Feed processing crashed ({FeedId})", feedId);
                }
                finally
                {
                    await CleanupCrashedFeedAsync(feedId);
                }
            },
            cts.Token);
    }

    public async Task<IReadOnlyList<string>> RenewLeasesAsync(CancellationToken ct)
    {
        var lostLeases = new List<string>();
        foreach (var feedId in _feedSubscriptions.Keys.ToList())
        {
            bool renewed;
            try
            {
                renewed = await _leaseStore.RenewAsync(ResourceType, feedId, _instanceId, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to renew lease for {FeedId}, treating as lost", feedId);
                renewed = false;
            }

            if (!renewed)
            {
                lostLeases.Add(feedId);

                // Immediately cancel the feed task to minimize split-brain window.
                // Without this, the feed continues processing until StopProcessingAsync
                // acquires the local lock, leaving a window where another instance
                // could acquire the expired lease and start a duplicate.
                if (_feedSubscriptions.TryGetValue(feedId, out var cts))
                {
                    try
                    {
                        await cts.CancelAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }

        return lostLeases;
    }

    public TimeSpan RenewalInterval => TimeSpan.FromSeconds(_leaseOptions.RenewalIntervalSeconds);

    public async Task StopAllAsync(CancellationToken ct)
    {
        var feedIds = _feedSubscriptions.Keys.ToList();
        foreach (var feedId in feedIds)
        {
            try
            {
                await StopAsync(feedId, deleteCursor: false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping feed {FeedId} during shutdown", feedId);
            }
        }
    }

    private async Task CleanupCrashedFeedAsync(string feedId)
    {
        try
        {
            if (_feedSubscriptions.TryRemove(feedId, out var cts))
            {
                cts.Dispose();
            }

            _feedTasks.TryRemove(feedId, out _);

            await _leaseStore.ReleaseAsync(ResourceType, feedId, _instanceId, CancellationToken.None);

            _logger.LogDebug("Cleaned up feed state for {FeedId}", feedId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning up crashed feed {FeedId}", feedId);
        }
    }
}
