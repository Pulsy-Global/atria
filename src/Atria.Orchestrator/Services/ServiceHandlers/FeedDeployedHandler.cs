using Atria.Business.Services.DataServices.Interfaces;
using Atria.Common.Messaging.ServiceBus;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Subjects.Feed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atria.Orchestrator.Services.ServiceHandlers;

public sealed class FeedDeployedHandler : ServiceBusHandler<FeedDeployedEvent>
{
    private readonly IServiceProvider _serviceProvider;

    public FeedDeployedHandler(
        IServiceBus serviceBus,
        IServiceProvider serviceProvider,
        ILogger<FeedDeployedHandler> logger)
        : base(serviceBus, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override string Subject => FeedSubjects.System.FeedDeployed;

    protected override string? QueueGroup => nameof(FeedDeployedHandler);

    protected override async Task HandleAsync(FeedDeployedEvent message, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var deployDataService = scope.ServiceProvider.GetRequiredService<IDeployDataService>();

        var feedId = Guid.Parse(message.FeedId);

        await deployDataService.ConfirmDeployedAsync(feedId, ct);

        Logger.LogInformation("Feed {FeedId} confirmed running by runtime", message.FeedId);
    }
}
