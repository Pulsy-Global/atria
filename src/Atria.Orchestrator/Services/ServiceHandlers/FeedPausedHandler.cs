using Atria.Business.Services.DataServices.Interfaces;
using Atria.Common.Messaging.ServiceBus;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;
using Atria.Contracts.Subjects.Feed;
using Atria.Core.Data.Entities.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atria.Orchestrator.Services.ServiceHandlers;

public sealed class FeedPausedHandler : ServiceBusHandler<FeedPausedEvent>
{
    private readonly IServiceProvider _serviceProvider;

    public FeedPausedHandler(
        IServiceBus serviceBus,
        IServiceProvider serviceProvider,
        ILogger<FeedPausedHandler> logger)
        : base(serviceBus, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override string Subject => FeedSubjects.System.FeedPaused;

    protected override string? QueueGroup => nameof(FeedPausedHandler);

    protected override async Task HandleAsync(FeedPausedEvent message, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var feedDataService = scope.ServiceProvider.GetRequiredService<IFeedDataService>();
        var deployDataService = scope.ServiceProvider.GetRequiredService<IDeployDataService>();

        if (!Guid.TryParse(message.FeedId, out var feedId))
        {
            Logger.LogWarning("Ignoring pause event with invalid feed id: {FeedId}", message.FeedId);
            return;
        }

        var feed = await feedDataService.GetFeedByIdAsync(feedId, ct);

        if (feed == null)
        {
            Logger.LogWarning("Feed not found for pause event: {FeedId}", message.FeedId);
            return;
        }

        if (!Guid.TryParse(message.DeployId, out var deployId))
        {
            Logger.LogWarning(
                "Ignoring pause event for feed {FeedId}: deploy id is missing or invalid ({DeployId})",
                message.FeedId,
                message.DeployId);

            return;
        }

        if (!feed.CurrentDeployId.HasValue)
        {
            Logger.LogWarning(
                "Ignoring pause event for feed {FeedId}: feed has no current deploy",
                message.FeedId);

            return;
        }

        if (feed.CurrentDeployId.Value != deployId)
        {
            Logger.LogInformation(
                "Ignoring stale pause event for feed {FeedId}: event deploy {DeployId}, current deploy {CurrentDeployId}",
                message.FeedId,
                deployId,
                feed.CurrentDeployId.Value);

            return;
        }

        var newStatus = MapPauseSourceToStatus(message.Source);
        var previousStatus = feed.Status;

        var deployUpdated = await UpdateCurrentDeployAsync(
            deployDataService,
            feed.Id,
            deployId,
            newStatus,
            message,
            ct);

        if (!deployUpdated)
        {
            Logger.LogWarning(
                "Ignoring pause event for feed {FeedId}: current deploy {DeployId} was not found",
                message.FeedId,
                deployId);

            return;
        }

        if (previousStatus == newStatus)
        {
            Logger.LogDebug(
                "Feed {FeedId} already in status {Status}, skipping update",
                message.FeedId,
                newStatus);
            return;
        }

        feed.Status = newStatus;
        await feedDataService.UpdateFeedAsync(feed, ct);

        Logger.LogInformation(
            "Feed {FeedId} status updated: {PreviousStatus} -> {NewStatus} (source: {Source})",
            message.FeedId,
            previousStatus,
            newStatus,
            message.Source);
    }

    private static FeedStatus MapPauseSourceToStatus(FeedPauseSource source)
    {
        return source switch
        {
            FeedPauseSource.Delivery => FeedStatus.Error,
            FeedPauseSource.BlockErrors => FeedStatus.Error,
            FeedPauseSource.ProcessingErrors => FeedStatus.Error,
            FeedPauseSource.User => FeedStatus.Paused,
            FeedPauseSource.Runtime => FeedStatus.Paused,
            _ => FeedStatus.Paused,
        };
    }

    private static async Task<bool> UpdateCurrentDeployAsync(
        IDeployDataService deployDataService,
        Guid feedId,
        Guid deployId,
        FeedStatus newStatus,
        FeedPausedEvent message,
        CancellationToken ct)
    {
        var deploy = await deployDataService.GetCurrentDeployAsync(feedId, ct);

        if (deploy == null)
        {
            return false;
        }

        if (deploy.Id != deployId)
        {
            return false;
        }

        if (newStatus == FeedStatus.Error)
        {
            deploy.MarkFailed(
                MapPauseSourceToErrorCode(message.Source),
                message.Source.ToString(),
                message.Reason);
        }
        else
        {
            deploy.Status = DeployStatus.None;
            deploy.UpdatedAt = DateTimeOffset.UtcNow;
            deploy.ClearError();
        }

        await deployDataService.UpdateDeployAsync(deploy, ct);

        return true;
    }

    private static DeployErrorCode MapPauseSourceToErrorCode(FeedPauseSource source)
    {
        return source switch
        {
            FeedPauseSource.Delivery => DeployErrorCode.WebhookUnavailable,
            FeedPauseSource.BlockErrors => DeployErrorCode.BlockDataUnavailable,
            FeedPauseSource.ProcessingErrors => DeployErrorCode.ProcessingFailed,
            _ => DeployErrorCode.OperationFailed,
        };
    }
}
