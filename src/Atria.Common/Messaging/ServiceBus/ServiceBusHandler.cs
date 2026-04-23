using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Atria.Common.Messaging.ServiceBus;

public abstract class ServiceBusHandler<T> : BackgroundService
{
    private readonly IServiceBus _serviceBus;
    protected ILogger Logger { get; }

    protected abstract string Subject { get; }
    protected abstract string? QueueGroup { get; }

    protected ServiceBusHandler(IServiceBus serviceBus, ILogger logger)
    {
        _serviceBus = serviceBus;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        Logger.LogInformation("Starting ServiceBus handler for {Subject}", Subject);

        await foreach (var message in _serviceBus.SubscribeWithMetadataAsync<T>(Subject, QueueGroup, ct))
        {
            try
            {
                await HandleAsync(message.Data, message.ReplyTo, ct);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing message for subject {Subject}", Subject);
            }
        }
    }

    protected virtual Task HandleAsync(T message, string? replyTo, CancellationToken ct)
        => HandleAsync(message, ct);

    protected abstract Task HandleAsync(T message, CancellationToken ct);
}
