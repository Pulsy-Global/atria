using Atria.Business.Services.Deployment.Interfaces;
using Atria.Common.Messaging.ServiceBus;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Subjects.Feed;
using Microsoft.Extensions.Logging;

namespace Atria.Business.Services.Deployment;

public class OutputEventPublisher : IOutputEventPublisher
{
    private readonly IServiceBus _serviceBus;
    private readonly ILogger<OutputEventPublisher> _logger;

    public OutputEventPublisher(IServiceBus serviceBus, ILogger<OutputEventPublisher> logger)
    {
        _serviceBus = serviceBus;
        _logger = logger;
    }

    public async Task PublishOutputUpdatedAsync(Guid outputId, CancellationToken ct = default)
    {
        var m = new OutputUpdated(Id: outputId.ToString());

        await _serviceBus.PublishAsync(FeedSubjects.System.DeliveryConfigUpdated, m, ct);
        _logger.LogDebug("Published OutputUpdated for {OutputId}", outputId);
    }
}
