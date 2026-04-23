using Atria.Common.Messaging.ServiceBus;
using Atria.Common.Models.Options;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;
using Atria.Contracts.Subjects.Feed;
using Atria.Feed.Ingestor.ChainClients;
using Atria.Feed.Ingestor.ChainClients.Interfaces;
using Atria.Feed.Ingestor.ChainClients.Models;
using Atria.Feed.Ingestor.Config.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Numerics;

namespace Atria.Feed.Ingestor.Services;

public class DataRequestService : BackgroundService
{
    private readonly ILogger<DataRequestService> _logger;
    private readonly EvmClient _evmClient;
    private readonly NetworkOptions _chainOptions;
    private readonly IServiceBus _serviceBus;

    public DataRequestService(
        ILogger<DataRequestService> logger,
        IClientFactory<EvmClient> evmClientFactory,
        IOptions<IngestorNetworkOptions> networksOptions,
        IServiceBus serviceBus)
    {
        _logger = logger;
        _evmClient = evmClientFactory.CreateClient();
        _chainOptions = networksOptions.Value.NetworkOptions;
        _serviceBus = serviceBus;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var testRequestSubject = FeedSubjects.System.TestData(_chainOptions.Id);
        await TestRequestSubscriptionAsync(testRequestSubject, ct);
    }

    private async Task TestRequestSubscriptionAsync(string subject, CancellationToken ct)
    {
        await foreach (var msg in _serviceBus.SubscribeWithMetadataAsync<FeedDataRequest>(
            subject,
            queueGroup: "test-data-workers",
            ct: ct))
        {
            if (msg.Data?.DataType == null || string.IsNullOrEmpty(msg.Data.BlockNumber))
            {
                continue;
            }

            if (string.IsNullOrEmpty(msg.ReplyTo))
            {
                _logger.LogWarning("Received test request without ReplyTo, skipping");
                continue;
            }

            try
            {
                await HandleTestRequest(msg.ReplyTo, msg.Data.DataType, msg.Data.BlockNumber, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle test request for {DataType} on block {Block}", msg.Data.DataType, msg.Data.BlockNumber);
            }
        }
    }

    private async Task HandleTestRequest(string replyTo, FeedDataType dataType, string blockNumber, CancellationToken ct)
    {
        var chain = _chainOptions.Id;

        try
        {
            var targetBlock = BigInteger.Parse(blockNumber);
            var blockData = await _evmClient.GetByBlockNumberAsync(targetBlock, ct);
            var data = ExtractDataByType(dataType, blockData);

            if (data != null)
            {
                await SendResponseAsync(replyTo, data, null, ct);
            }
            else
            {
                var errorMessage = $"Unsupported FeedDataType: {dataType}";
                _logger.LogWarning(errorMessage);
                await SendResponseAsync(replyTo, null, errorMessage, ct);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to handle test request for {dataType} on block {blockNumber} for {chain}: {ex.Message}";
            _logger.LogError(ex, errorMessage);
            await SendResponseAsync(replyTo, null, errorMessage, ct);
        }
    }

    private object? ExtractDataByType(FeedDataType dataType, BlockData blockData)
    {
        return dataType switch
        {
            FeedDataType.Transactions => blockData.Block,
            FeedDataType.Logs => blockData.Logs,
            FeedDataType.Traces => blockData.Traces,
            _ => null
        };
    }

    private async Task SendResponseAsync(string replyTo, object? data, string? error = null, CancellationToken ct = default)
    {
        var response = new FeedDataResponse(error == null, data, error);
        await _serviceBus.PublishAsync(replyTo, response, ct);
    }
}
