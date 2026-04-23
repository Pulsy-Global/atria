namespace Atria.Common.Messaging.RequestReply;

public interface IRequestClient
{
    Task<TResponse?> SendAsync<TRequest, TResponse>(
        string subject,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken ct = default);
}
