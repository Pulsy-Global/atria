using Atria.Common.Messaging.Core;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Atria.Common.Messaging.RequestReply;

public sealed class RequestClient : IRequestClient
{
    private readonly NatsConnection _connection;
    private readonly ILogger<RequestClient> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(60);

    public RequestClient(NatsConnectionManager connectionManager, ILogger<RequestClient> logger)
    {
        _connection = connectionManager.Connection;
        _logger = logger;
    }

    public async Task<TResponse?> SendAsync<TRequest, TResponse>(
        string subject,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;

        _logger.LogTrace("Sending request to {Subject} (Timeout={Timeout}s)", subject, effectiveTimeout.TotalSeconds);

        try
        {
            var replyOpts = new NatsSubOpts { Timeout = effectiveTimeout };
            var response = await _connection.RequestAsync<TRequest, TResponse>(
                subject,
                request,
                replyOpts: replyOpts,
                cancellationToken: ct);

            if (response.Data != null)
            {
                _logger.LogTrace("Received response from {Subject}", subject);
                return response.Data;
            }

            _logger.LogWarning("Received null response from {Subject}", subject);
            return default;
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Timeout waiting for response from {Subject}", subject);
            return default;
        }
    }
}
