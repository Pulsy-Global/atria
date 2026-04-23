using Atria.Common.Messaging.Models;
using Atria.Common.Messaging.ServiceBus;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;
using Atria.Contracts.Subjects.Feed;
using Atria.Feed.Delivery.FeedPipeline.Interfaces;
using Atria.Pipeline.Interfaces;
using Atria.Pipeline.Options;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Atria.Feed.Delivery.Services;

public class FeedDeliveryService : BackgroundService
{
    private const string ConsumerPrefix = "feed-delivery";
    private const string ResourceType = LeaseResources.FeedDelivery;
    private const int MaxDeliver = 30;

    private readonly IFeedPipeline _feedPipeline;
    private readonly IFeedSubscriber _feedSubscriber;
    private readonly IServiceBus _serviceBus;
    private readonly LeaseStore _leaseStore;
    private readonly LeaseOptions _leaseOptions;
    private readonly ILogger<FeedDeliveryService> _logger;
    private readonly string _instanceId;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _feedCts = new();
    private readonly ConcurrentDictionary<string, Task> _feedTasks = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _feedLocks = new();

    public FeedDeliveryService(
        IFeedPipeline feedPipeline,
        IFeedSubscriber feedSubscriber,
        IServiceBus serviceBus,
        LeaseStore leaseStore,
        IOptions<LeaseOptions> leaseOptions,
        ILogger<FeedDeliveryService> logger)
    {
        _feedPipeline = feedPipeline;
        _feedSubscriber = feedSubscriber;
        _serviceBus = serviceBus;
        _leaseStore = leaseStore;
        _leaseOptions = leaseOptions.Value;
        _logger = logger;
        _instanceId = $"{Environment.MachineName}-{Guid.NewGuid():N}"[..32];
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Feed Delivery Service, releasing all leases");

        var feedIds = _feedCts.Keys.ToList();
        foreach (var feedId in feedIds)
        {
            try
            {
                await StopFeedConsumerAsync(feedId, deleteFromNats: false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error releasing lease for feed {FeedId} during shutdown", feedId);
            }
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        foreach (var cts in _feedCts.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _feedCts.Clear();
        _feedTasks.Clear();

        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting Feed Delivery Service, instance {InstanceId}", _instanceId);

        await Task.WhenAll(
            HandleDeployRequests(ct),
            HandleDeleteRequests(ct),
            HandlePauseRequests(ct),
            HandleDeliverTestOutputRequests(ct),
            RunLeaseRenewalLoopAsync(ct));
    }

    private static string GetConsumerName(string feedId)
        => $"{ConsumerPrefix}-{feedId}";

    private SemaphoreSlim GetFeedLock(string feedId)
        => _feedLocks.GetOrAdd(feedId, _ => new SemaphoreSlim(1, 1));

    private async Task HandleDeployRequests(CancellationToken ct)
    {
        await foreach (var request in _serviceBus.SubscribeAsync<FeedDeployRequest>(
            FeedSubjects.System.DeployRequest,
            queueGroup: "delivery-deploy-workers",
            ct: ct))
        {
            try
            {
                await TryStartFeedWithLeaseAsync(request.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting delivery consumer for feed {FeedId}", request.Id);
            }
        }
    }

    private async Task TryStartFeedWithLeaseAsync(string feedId, CancellationToken ct)
    {
        var feedLock = GetFeedLock(feedId);
        await feedLock.WaitAsync(ct);
        try
        {
            if (_feedCts.ContainsKey(feedId))
            {
                return;
            }

            var acquired = await _leaseStore.TryAcquireAsync(ResourceType, feedId, _instanceId, ct);

            if (!acquired)
            {
                _logger.LogDebug("Failed to acquire lease for feed {FeedId}, another instance owns it", feedId);
                return;
            }

            StartFeedConsumer(feedId, ct);
            _logger.LogInformation("Started delivery consumer for feed {FeedId}", feedId);
        }
        finally
        {
            feedLock.Release();
        }
    }

    private async Task HandleDeleteRequests(CancellationToken ct)
    {
        await foreach (var request in _serviceBus.SubscribeAsync<FeedDeleteRequest>(
            FeedSubjects.System.DeleteRequest,
            ct: ct))
        {
            try
            {
                var feedLock = GetFeedLock(request.Id);
                await feedLock.WaitAsync(ct);
                try
                {
                    if (!_feedCts.ContainsKey(request.Id))
                    {
                        continue;
                    }

                    await StopFeedConsumerAsync(request.Id, deleteFromNats: true, ct);
                }
                finally
                {
                    feedLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting delivery consumer for feed {FeedId}", request.Id);
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
                var feedLock = GetFeedLock(request.Id);
                await feedLock.WaitAsync(ct);
                try
                {
                    if (!_feedCts.ContainsKey(request.Id))
                    {
                        continue;
                    }

                    await StopFeedConsumerAsync(request.Id, deleteFromNats: false, ct);
                    _logger.LogInformation("Stopped delivery consumer for paused feed {FeedId}", request.Id);
                }
                finally
                {
                    feedLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling pause for feed {FeedId}", request.Id);
            }
        }
    }

    private async Task HandleDeliverTestOutputRequests(CancellationToken ct)
    {
        await foreach (var msg in _serviceBus.SubscribeWithMetadataAsync<DeliverTestOutputRequest>(
            FeedSubjects.System.DeliverTestOutput,
            queueGroup: "delivery-test-workers",
            ct: ct))
        {
            if (string.IsNullOrEmpty(msg.ReplyTo) || msg.Data == null)
            {
                continue;
            }

            try
            {
                if (msg.Data.OutputIds != null)
                {
                    await _feedPipeline.ExecutePipelineAsync(
                        msg.Data.FeedId,
                        msg.Data.OutputIds,
                        msg.Data.Data,
                        isTestExecution: true,
                        ct);
                }

                await _serviceBus.PublishAsync(msg.ReplyTo, new DeliverTestOutputResponse(true), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering test output for {FeedId}", msg.Data.FeedId);
                await _serviceBus.PublishAsync(msg.ReplyTo, new DeliverTestOutputResponse(false, ex.Message), ct);
            }
        }
    }

    private async Task RunLeaseRenewalLoopAsync(CancellationToken ct)
    {
        var interval = TimeSpan.FromSeconds(_leaseOptions.RenewalIntervalSeconds);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, ct);

                var lostFeeds = new List<string>();
                var ownedFeeds = _feedCts.Keys.ToList();

                foreach (var feedId in ownedFeeds)
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
                        _logger.LogError(ex, "Failed to renew lease for feed {FeedId}, treating as lost", feedId);
                        renewed = false;
                    }

                    if (!renewed)
                    {
                        lostFeeds.Add(feedId);

                        // Immediately cancel to minimize split-brain window
                        if (_feedCts.TryGetValue(feedId, out var cts))
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

                foreach (var feedId in lostFeeds)
                {
                    _logger.LogWarning("Lost lease for feed {FeedId}, stopping consumer", feedId);
                    await StopFeedConsumerAsync(feedId, deleteFromNats: false, ct);
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

    private void StartFeedConsumer(string feedId, CancellationToken globalCt)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(globalCt);

        if (!_feedCts.TryAdd(feedId, cts))
        {
            cts.Dispose();
            return;
        }

        var consumerName = GetConsumerName(feedId);
        _feedTasks[feedId] = Task.Run(
            async () =>
            {
                try
                {
                    await ProcessFeedMessagesAsync(feedId, consumerName, cts.Token);
                }
                catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                {
                    _logger.LogDebug("Feed delivery consumer {FeedId} stopped", feedId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Feed delivery consumer crashed ({FeedId})", feedId);
                }
                finally
                {
                    await CleanupCrashedConsumerAsync(feedId);
                }
            },
            cts.Token);
    }

    private async Task CleanupCrashedConsumerAsync(string feedId)
    {
        try
        {
            if (_feedCts.TryRemove(feedId, out var cts))
            {
                cts.Dispose();
            }

            _feedTasks.TryRemove(feedId, out _);

            await _leaseStore.ReleaseAsync(ResourceType, feedId, _instanceId, CancellationToken.None);

            _logger.LogDebug("Cleaned up consumer state for feed {FeedId}", feedId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning up crashed consumer for feed {FeedId}", feedId);
        }
    }

    private async Task StopFeedConsumerAsync(string feedId, bool deleteFromNats, CancellationToken ct)
    {
        if (_feedCts.TryRemove(feedId, out var cts))
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        if (_feedTasks.TryRemove(feedId, out var task))
        {
            try
            {
                await task.WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch
            {
                // Ignore
            }
        }

        await _leaseStore.ReleaseAsync(ResourceType, feedId, _instanceId, ct);

        if (deleteFromNats)
        {
            await _feedSubscriber.DeleteConsumerAsync(GetConsumerName(feedId));
        }
    }

    private async Task ProcessFeedMessagesAsync(string feedId, string consumerName, CancellationToken ct)
    {
        await foreach (var message in _feedSubscriber.SubscribeFeedAsync<FeedOutputData>(feedId, consumerName, ct))
        {
            var feedOutput = message.Data;
            if (feedOutput.OutputIds == null)
            {
                await message.AckAsync();
                continue;
            }

            try
            {
                await _feedPipeline.ExecutePipelineAsync(
                    feedOutput.FeedId,
                    feedOutput.OutputIds,
                    feedOutput.Data,
                    feedOutput.IsTestExecution,
                    ct);

                await message.AckAsync();
            }
            catch (Exception ex)
            {
                await HandleDeliveryErrorAsync(message, feedOutput.FeedId, ex, ct);
            }
        }
    }

    private async Task HandleDeliveryErrorAsync(
        MessagingEnvelope<FeedOutputData> message,
        string feedId,
        Exception ex,
        CancellationToken ct)
    {
        var attempt = (int)message.DeliveryAttempt;

        if (attempt >= MaxDeliver)
        {
            _logger.LogError(ex, "Feed {FeedId}: delivery failed after {Attempts} attempts, pausing", feedId, attempt);

            await _serviceBus.PublishAsync(
                FeedSubjects.System.PauseRequest,
                new FeedPauseRequest(feedId, FeedPauseSource.Delivery, $"Delivery failed: {ex.Message}"),
                ct);

            await message.AckAsync();
        }
        else
        {
            _logger.LogWarning(ex, "Feed {FeedId}: delivery failed, attempt {Attempt}/{Max}", feedId, attempt, MaxDeliver);
            await message.NakAsync(TimeSpan.FromSeconds(30));
        }
    }
}
