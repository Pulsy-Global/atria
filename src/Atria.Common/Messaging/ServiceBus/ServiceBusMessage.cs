namespace Atria.Common.Messaging.ServiceBus;

public sealed class ServiceBusMessage<T>
{
    public T Data { get; }

    public string? ReplyTo { get; }

    public ServiceBusMessage(T data, string? replyTo)
    {
        Data = data;
        ReplyTo = replyTo;
    }
}
