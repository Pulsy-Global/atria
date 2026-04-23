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

        var feed = await feedDataService.GetFeedByIdAsync(Guid.Parse(message.FeedId), ct);

        if (feed == null)
        {
            Logger.LogWarning("Feed not found for pause event: {FeedId}", message.FeedId);
            return;
        }

        var newStatus = MapPauseSourceToStatus(message.Source);
        var previousStatus = feed.Status;

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
}
