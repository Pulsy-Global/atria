namespace Atria.Common.Messaging.ServiceBus;

public interface IServiceBus
{
    Task PublishAsync<T>(string subject, T message, CancellationToken ct = default);

    IAsyncEnumerable<T> SubscribeAsync<T>(
        string subject,
        string? queueGroup = null,
        CancellationToken ct = default);

    IAsyncEnumerable<ServiceBusMessage<T>> SubscribeWithMetadataAsync<T>(
        string subject,
        string? queueGroup = null,
        CancellationToken ct = default);
}
