using NATS.Client.JetStream;

namespace Atria.Common.Messaging.Models;

public sealed class MessagingEnvelope<T>
{
    private readonly INatsJSMsg<T>? _jsMsg;

    public T Data { get; }

    public string? ReplyTo { get; }

    public ulong DeliveryAttempt => _jsMsg?.Metadata?.NumDelivered ?? 1;

    public MessagingEnvelope(T data, INatsJSMsg<T>? jsMsg, string? replyTo)
    {
        Data = data;
        _jsMsg = jsMsg;
        ReplyTo = replyTo;
    }

    public ValueTask AckAsync()
    {
        if (_jsMsg != null)
        {
            return _jsMsg.AckAsync();
        }

        return default;
    }

    public ValueTask NakAsync()
    {
        if (_jsMsg != null)
        {
            return _jsMsg.NakAsync();
        }

        return default;
    }

    public ValueTask NakAsync(TimeSpan delay)
    {
        if (_jsMsg != null)
        {
            return _jsMsg.NakAsync(delay: delay);
        }

        return default;
    }
}
