using Atria.Business.Services.DataServices.Interfaces;
using Atria.Common.Exceptions;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Feeds;
using Atria.Orchestrator.Config.Options;
using Atria.Orchestrator.Observability;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Atria.Orchestrator.Services.ServiceHandlers;

public sealed class FeedDeploymentReconciler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LeaseStore _leaseStore;
    private readonly OrchestratorOptions _orchestratorOptions;
    private readonly ILogger<FeedDeploymentReconciler> _logger;
    private readonly OrchestratorMetricsRecorder _metrics;

    public FeedDeploymentReconciler(
        IServiceProvider serviceProvider,
        LeaseStore leaseStore,
        IOptions<OrchestratorOptions> orchestratorOptions,
        OrchestratorMetricsRecorder metrics,
        ILogger<FeedDeploymentReconciler> logger)
    {
        _serviceProvider = serviceProvider;
        _leaseStore = leaseStore;
        _orchestratorOptions = orchestratorOptions.Value;
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("{Handler} started", nameof(FeedDeploymentReconciler));

        while (!ct.IsCancellationRequested)
        {
            var startedAt = Stopwatch.GetTimestamp();
            var succeeded = false;
            try
            {
                await CheckPendingDeploysAsync(ct);
                await CheckRunningFeedsLeaseAsync(ct);
                succeeded = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in deploy check cycle");
            }
            finally
            {
                _metrics.RecordReconciliation(succeeded, Stopwatch.GetElapsedTime(startedAt));
            }

            var delay = TimeSpan.FromSeconds(_orchestratorOptions.Provisioning.PoolingIntervalSec);
            await Task.Delay(delay, ct);
        }
    }

    private async Task CheckPendingDeploysAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();

        var feedDataService = scope.ServiceProvider.GetRequiredService<IFeedDataService>();
        var deployService = scope.ServiceProvider.GetRequiredService<IDeployDataService>();

        var feeds = await feedDataService.GetFeedsAsync(x => x.Status == FeedStatus.Pending, ct);
        _metrics.SetPendingDeployments(feeds.Count);

        foreach (var feed in feeds)
        {
            try
            {
                await ReconcilePendingFeedAsync(feed, feedDataService, deployService, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconcile pending feed {FeedId}", feed.Id);
            }
        }
    }

    private async Task ReconcilePendingFeedAsync(
        Feed feed,
        IFeedDataService feedDataService,
        IDeployDataService deployService,
        CancellationToken ct)
    {
        var deploy = await deployService.GetCurrentDeployAsync(feed.Id, ct);

        if (deploy == null)
        {
            return;
        }

        var feedId = feed.Id.ToString();
        var lease = await _leaseStore.GetLeaseAsync(LeaseResources.FeedRuntime, feedId, ct);

        if (lease is not null)
        {
            _logger.LogInformation("Feed {FeedId} has active lease, confirming as running", feedId);
            await deployService.ConfirmDeployedAsync(feed.Id, deploy.Id, ct);
            _metrics.RecordReconciliationAction("confirm_running", "runtime_lease_present");
            return;
        }

        var pendingDuration = DateTimeOffset.UtcNow - deploy.UpdatedAt!.Value;
        var deployStuckTimeout = TimeSpan.FromSeconds(_orchestratorOptions.DeployStuckIntervalSec);
        var retryInterval = TimeSpan.FromSeconds(_orchestratorOptions.DeployRetryIntervalSec);

        if (pendingDuration > deployStuckTimeout)
        {
            _logger.LogWarning("Deploy {DeployId} stuck in pending, marking as failed", deploy.Id);

            deploy.MarkFailed(
                DeployErrorCode.RuntimeUnavailable,
                nameof(DeployErrorCode.RuntimeUnavailable),
                "No runtime became available before the deployment timeout.");

            feed.Status = FeedStatus.Error;

            await deployService.UpdateDeployAsync(deploy, ct);
            await feedDataService.UpdateFeedAsync(feed, ct);
            _metrics.RecordReconciliationAction("mark_failed", "deploy_timeout");
        }
        else if (pendingDuration > retryInterval)
        {
            _logger.LogInformation(
                "Feed {FeedId} pending without lease for {Duration}s, republishing deploy",
                feedId,
                pendingDuration.TotalSeconds);

            await deployService.PublishDeployRequestAsync(feed.Id, ct);
            _metrics.RecordReconciliationAction("republish", "deploy_retry_interval");
        }
    }

    private async Task CheckRunningFeedsLeaseAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();

        var feedDataService = scope.ServiceProvider.GetRequiredService<IFeedDataService>();
        var deployService = scope.ServiceProvider.GetRequiredService<IDeployDataService>();

        var feeds = await feedDataService.GetFeedsAsync(x => x.Status == FeedStatus.Running, ct);

        await Parallel.ForEachAsync(
            feeds,
            new ParallelOptions { MaxDegreeOfParallelism = 20, CancellationToken = ct },
            async (feed, token) =>
            {
                try
                {
                    await ReconcileRunningFeedAsync(feed, deployService, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reconcile running feed {FeedId}", feed.Id);
                }
            });
    }

    private async Task ReconcileRunningFeedAsync(
        Feed feed,
        IDeployDataService deployService,
        CancellationToken ct)
    {
        var feedId = feed.Id.ToString();
        var runtimeLease = await _leaseStore.GetLeaseAsync(LeaseResources.FeedRuntime, feedId, ct);

        if (runtimeLease is null)
        {
            _logger.LogInformation(
                "Feed {FeedId} has no active runtime lease (expired or missing), redeploying",
                feedId);

            try
            {
                await deployService.ExecuteDeploymentAsync(feed.Id, ct);

                _metrics.RecordReconciliationAction("redeploy", "runtime_lease_missing");
            }
            catch (CursorBehindTailException ex)
            {
                var markedFailed = await deployService.FailCurrentDeploymentAsync(
                    feed.Id,
                    feed.CurrentDeployId,
                    DeployErrorCode.BlockDataUnavailable,
                    nameof(CursorBehindTailException),
                    ex.Message,
                    ct);

                if (markedFailed)
                {
                    _logger.LogError(
                        ex,
                        "Feed {FeedId} cannot be recovered because its cursor is behind the retained chain tail; marked as failed",
                        feedId);
                    _metrics.RecordReconciliationAction("mark_failed", "cursor_behind_tail");
                }
            }

            return;
        }

        var deliveryLease = await _leaseStore.GetLeaseAsync(LeaseResources.FeedDelivery, feedId, ct);

        if (deliveryLease is null)
        {
            _logger.LogInformation(
                "Feed {FeedId} has no active delivery lease, republishing deploy request",
                feedId);

            await deployService.PublishDeployRequestAsync(feed.Id, ct);
            _metrics.RecordReconciliationAction("republish", "delivery_lease_missing");
        }
    }
}
