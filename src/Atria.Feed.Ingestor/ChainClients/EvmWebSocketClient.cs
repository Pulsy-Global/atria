using Atria.Common.Models.Options;
using Atria.Feed.Ingestor.ChainClients.Interfaces;
using Microsoft.Extensions.Logging;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using System.Threading.Channels;

namespace Atria.Feed.Ingestor.ChainClients;

public class EvmWebSocketClient : IEvmWebSocketClient
{
    private readonly ILogger<EvmWebSocketClient> _logger;
    private readonly NetworkOptions _chainOptions;

    public EvmWebSocketClient(
        ILogger<EvmWebSocketClient> logger,
        NetworkOptions chainOptions)
    {
        _logger = logger;
        _chainOptions = chainOptions;
    }

    public async Task ListenAsync(ChannelWriter<bool> signal, TimeSpan inactivityTimeout, CancellationToken ct)
    {
        var ws = new StreamingWebSocketClient(_chainOptions.NodeWsUrl);
        var subscription = new EthNewBlockHeadersObservableSubscription(ws);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(inactivityTimeout);

        subscription.GetSubscriptionDataResponsesAsObservable()
            .Subscribe(
                blockHeader =>
                {
                    _logger.LogDebug("Received block {Block} via WebSocket for {Chain}", blockHeader.Number.Value, _chainOptions.Id);
                    timeoutCts.CancelAfter(inactivityTimeout);
                    signal.TryWrite(true);
                },
                error => completion.TrySetException(error));

        try
        {
            _logger.LogInformation("Connecting WebSocket to {WsUrl} for {Chain}", _chainOptions.NodeWsUrl, _chainOptions.Id);

            await ws.StartAsync();
            await subscription.SubscribeAsync();

            _logger.LogInformation("WebSocket connected for {Chain}", _chainOptions.Id);

            using var reg = timeoutCts.Token.Register(() => completion.TrySetCanceled(ct));
            await completion.Task;
        }
        finally
        {
            await DisposeQuietlyAsync(subscription, ws);
        }
    }

    private async Task DisposeQuietlyAsync(
        EthNewBlockHeadersObservableSubscription subscription,
        StreamingWebSocketClient ws)
    {
        try
        {
            await subscription.UnsubscribeAsync();
            await ws.StopAsync();
            ws.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing WebSocket for {Chain}", _chainOptions.Id);
        }
    }
}
