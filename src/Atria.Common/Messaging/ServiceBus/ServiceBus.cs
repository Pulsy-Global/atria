using Atria.Common.Messaging.Core;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Atria.Common.Messaging.ServiceBus;

public sealed class ServiceBus : IServiceBus
{
    private readonly NatsConnection _connection;
    private readonly ILogger<ServiceBus> _logger;

    public ServiceBus(NatsConnectionManager connectionManager, ILogger<ServiceBus> logger)
    {
        _connection = connectionManager.Connection;
        _logger = logger;
    }

    public async Task PublishAsync<T>(string subject, T message, CancellationToken ct = default)
    {
        await _connection.PublishAsync(subject, message, cancellationToken: ct);
        _logger.LogTrace("Published message to {Subject}", subject);
    }

    public async IAsyncEnumerable<T> SubscribeAsync<T>(
        string subject,
        string? queueGroup = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation("Subscribing to {Subject} (QueueGroup={QueueGroup})", subject, queueGroup ?? "none");

        await foreach (var msg in _connection.SubscribeAsync<T>(subject, queueGroup: queueGroup, cancellationToken: ct))
        {
            if (msg.Data != null)
            {
                yield return msg.Data;
            }
        }
    }

    public async IAsyncEnumerable<ServiceBusMessage<T>> SubscribeWithMetadataAsync<T>(
        string subject,
        string? queueGroup = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation("Subscribing to {Subject} (QueueGroup={QueueGroup})", subject, queueGroup ?? "none");

        await foreach (var msg in _connection.SubscribeAsync<T>(subject, queueGroup: queueGroup, cancellationToken: ct))
        {
            if (msg.Data != null)
            {
                yield return new ServiceBusMessage<T>(msg.Data, msg.ReplyTo);
            }
        }
    }
}
