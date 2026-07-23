using Atria.Orchestrator.Config.Options;
using Atria.Orchestrator.Models.Business;
using Atria.Orchestrator.Models.Dto.Feed;
using Atria.Orchestrator.Observability;
using Atria.Orchestrator.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Atria.Orchestrator.Services.Deployment;

public class ProvisioningService : BackgroundService
{
    private readonly ILogger<ProvisioningService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly OrchestratorOptions _orchestratorOptions;
    private readonly TimeSpan _scanInterval;
    private readonly OrchestratorMetricsRecorder _metrics;

    private bool _isStarted;

    public ProvisioningService(
        ILogger<ProvisioningService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<OrchestratorOptions> orchestratorOptions,
        OrchestratorMetricsRecorder metrics)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _orchestratorOptions = orchestratorOptions.Value;
        _metrics = metrics;

        _scanInterval = TimeSpan.FromSeconds(orchestratorOptions.Value.Provisioning.PoolingIntervalSec);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_orchestratorOptions.Provisioning.Enabled)
        {
            return;
        }

        _logger.LogInformation("Starting FeedLocalDeployService with scan interval: {Interval}", _scanInterval);

        await PerformInitialScanAsync(ct);

        _isStarted = true;

        using var timer = new PeriodicTimer(_scanInterval);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(ct);
                await PerformPeriodicScanAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic scan");
            }
        }
    }

    private async Task<ScanResult<FeedDeployment>> ScanAndCompareAsync(CancellationToken ct = default)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var succeeded = false;

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var manifestScanner = scope.ServiceProvider.GetRequiredService<IManifestScanner>();
            var outputManager = scope.ServiceProvider.GetRequiredService<IOutputProvisioningManager>();
            var feedManager = scope.ServiceProvider.GetRequiredService<IFeedProvisioningManager>();

            var manifests = await manifestScanner.ScanDirectoryAsync<FeedManifest>(
                _orchestratorOptions.Provisioning.GetFeedsPath(),
                "manifest.json",
                feedManager.ValidateFeedManifest);

            await outputManager.ExecuteProvisioningAsync(ct);

            var result = await feedManager.CompareFeedsWithManifestsAsync(manifests, ct);
            _metrics.RecordManifestChanges(result.Added.Count, result.Modified.Count, result.RemovedIds.Count);
            succeeded = true;

            return result;
        }
        finally
        {
            _metrics.RecordProvisioningScan(succeeded, Stopwatch.GetElapsedTime(startedAt));
        }
    }

    private async Task ProcessDeploymentChangesAsync(ScanResult<FeedDeployment> scanResult, CancellationToken ct = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var manifestScanner = scope.ServiceProvider.GetRequiredService<IManifestScanner>();
        var feedManager = scope.ServiceProvider.GetRequiredService<IFeedProvisioningManager>();

        if (!manifestScanner.HasChanges(scanResult))
        {
            _logger.LogDebug("No changes detected, skipping processing");
            return;
        }

        foreach (var feedDeployment in scanResult.Added)
        {
            await ProcessAddedFeedAsync(feedDeployment, feedManager, ct);
        }

        foreach (var feedDeployment in scanResult.Modified)
        {
            await ProcessModifiedFeedAsync(feedDeployment, feedManager, ct);
        }

        foreach (var feedId in scanResult.RemovedIds)
        {
            await ProcessRemovedFeedAsync(feedId, feedManager, ct);
        }
    }

    private async Task PerformInitialScanAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var outputManager = scope.ServiceProvider.GetRequiredService<IOutputProvisioningManager>();

        try
        {
            _logger.LogInformation("Performing initial deployment scan");

            await outputManager.ExecuteProvisioningAsync(ct);

            var scanResult = await ScanAndCompareAsync(ct);
            await ProcessDeploymentChangesAsync(scanResult, ct);

            _logger.LogInformation("Initial deployment scan completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform initial deployment scan");
            throw;
        }
    }

    private async Task PerformPeriodicScanAsync()
    {
        if (!_isStarted)
        {
            return;
        }

        try
        {
            _logger.LogDebug("Performing periodic deployment scan");

            var scanResult = await ScanAndCompareAsync();
            await ProcessDeploymentChangesAsync(scanResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic deployment scan");
        }
    }

    private async Task ProcessAddedFeedAsync(FeedDeployment feedDeployment, IFeedProvisioningManager feedManager, CancellationToken ct)
    {
        var succeeded = false;
        try
        {
            await feedManager.CreateFeedFromManifestAsync(feedDeployment.Manifest, feedDeployment.Hash, ct);
            succeeded = true;

            _logger.LogInformation(
                "New feed deployed: {Name} v{Version}",
                feedDeployment.Manifest.Name,
                feedDeployment.Manifest.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to deploy new feed: {Name} v{Version}",
                feedDeployment.Manifest.Name,
                feedDeployment.Manifest.Version);
        }
        finally
        {
            _metrics.RecordProvisioningAction("create", succeeded);
        }
    }

    private async Task ProcessModifiedFeedAsync(FeedDeployment feedDeployment, IFeedProvisioningManager feedManager, CancellationToken ct)
    {
        var succeeded = false;
        try
        {
            await feedManager.UpdateFeedFromManifestAsync(feedDeployment.Manifest, feedDeployment.Hash, ct);
            succeeded = true;

            _logger.LogInformation(
                "Feed redeployed: {Name} v{Version}",
                feedDeployment.Manifest.Name,
                feedDeployment.Manifest.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to redeploy feed: {Name} v{Version}",
                feedDeployment.Manifest.Name,
                feedDeployment.Manifest.Version);
        }
        finally
        {
            _metrics.RecordProvisioningAction("update", succeeded);
        }
    }

    private async Task ProcessRemovedFeedAsync(Guid feedId, IFeedProvisioningManager feedManager, CancellationToken ct)
    {
        var succeeded = false;
        try
        {
            await feedManager.DeleteFeedAsync(feedId, ct);
            succeeded = true;
            _logger.LogInformation("Feed undeployed: {FeedId}", feedId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to undeploy feed: {FeedId}", feedId);
        }
        finally
        {
            _metrics.RecordProvisioningAction("delete", succeeded);
        }
    }
}
