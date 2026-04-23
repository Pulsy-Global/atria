using Atria.Common.Messaging.ServiceBus;
using Atria.Common.Models.Options;
using Atria.Contracts.Events.Blockchain;
using Atria.Contracts.Subjects.Blockchain;
using Atria.Feed.Ingestor.Config.Options;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atria.Feed.Ingestor.Services;

public sealed class ChainHeadRequestHandler : BackgroundService
{
    private readonly IServiceBus _serviceBus;
    private readonly ChainStateStore _chainStateStore;
    private readonly NetworkOptions _chainOptions;
    private readonly ILogger<ChainHeadRequestHandler> _logger;

    public ChainHeadRequestHandler(
        IServiceBus serviceBus,
        ChainStateStore chainStateStore,
        IOptions<IngestorNetworkOptions> networksOptions,
        ILogger<ChainHeadRequestHandler> logger)
    {
        _serviceBus = serviceBus;
        _chainStateStore = chainStateStore;
        _chainOptions = networksOptions.Value.NetworkOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var subject = Blockchain.Subjects.ChainHeadRequest(_chainOptions.Id);

        _logger.LogInformation("Starting chain head request handler for {ChainId}", _chainOptions.Id);

        await foreach (var msg in _serviceBus.SubscribeWithMetadataAsync<ChainHeadRequest>(
            subject,
            queueGroup: "chain-head-workers",
            ct: ct))
        {
            if (string.IsNullOrEmpty(msg.ReplyTo))
            {
                _logger.LogWarning("Received chain head request without ReplyTo, skipping");
                continue;
            }

            try
            {
                var head = await _chainStateStore.GetHeadAsync(_chainOptions.Id, ct);

                var response = head.HasValue
                    ? new ChainHeadResponse(true, (ulong)head.Value)
                    : new ChainHeadResponse(false, null, "Head not available");

                await _serviceBus.PublishAsync(msg.ReplyTo, response, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle chain head request");
                await _serviceBus.PublishAsync(
                    msg.ReplyTo,
                    new ChainHeadResponse(false, null, ex.Message),
                    ct);
            }
        }
    }
}
