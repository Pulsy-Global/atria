using Atria.Common.Messaging.RequestReply;
using Atria.Common.Messaging.ServiceBus;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Subjects.Feed;
using Atria.Feed.Runtime.Engine;
using Atria.Feed.Runtime.Engine.Exceptions;
using Atria.Feed.Runtime.Engine.Models;
using Atria.Pipeline.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Numerics;

namespace Atria.Feed.Runtime.Services;

public sealed class FeedTestService : BackgroundService
{
    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(15);

    private readonly IServiceBus _serviceBus;
    private readonly IRequestClient _requestClient;
    private readonly IBlockProvider _blockProvider;
    private readonly FeedManager _feedManager;
    private readonly ILogger<FeedTestService> _logger;

    public FeedTestService(
        FeedManager manager,
        IServiceBus serviceBus,
        IRequestClient requestClient,
        IBlockProvider blockProvider,
        ILogger<FeedTestService> logger)
    {
        _serviceBus = serviceBus;
        _requestClient = requestClient;
        _blockProvider = blockProvider;
        _feedManager = manager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var subject = FeedSubjects.System.TestRequest;

        _logger.LogInformation("FeedTestService listening on {Subject} (queue=test-workers)", subject);

        try
        {
            await foreach (var msg in _serviceBus.SubscribeWithMetadataAsync<FeedTestRequest>(subject, "test-workers", ct))
            {
                await HandleMessageAsync(msg, ct);
            }

            _logger.LogWarning("FeedTestService subscription loop exited without cancellation");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("FeedTestService stopping (cancellation requested)");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "FeedTestService terminated with unhandled exception");
            throw;
        }
    }

    private async Task HandleMessageAsync(ServiceBusMessage<FeedTestRequest> msg, CancellationToken ct)
    {
        FeedTestResponse? resp = null;
        string? replyTo = msg.ReplyTo;
        var request = msg.Data;
        var feedId = request?.DeployRequest?.Id;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("Test request {FeedId} received", feedId);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_requestTimeout);
        var requestCt = cts.Token;

        try
        {
            if (request != null && request.DeployRequest != null)
            {
                var feedRuntime = new FeedRuntime
                {
                    Id = request.DeployRequest.Id,
                    ChainId = request.DeployRequest.ChainId,
                    DataType = request.DeployRequest.FeedDataType,
                    FilterLangKind = request.DeployRequest.FilterLangKind,
                    FunctionLangKind = request.DeployRequest.FunctionLangKind,
                    FilterCode = request.DeployRequest.FilterCode,
                    Type = request.DeployRequest.Type,
                    FunctionCode = request.DeployRequest.FunctionCode,
                    OutputIds = request.DeployRequest.OutputIds,
                    EkvNamespace = request.DeployRequest.EkvNamespace,
                };

                var dataType = feedRuntime.DataType.ToString().ToLowerInvariant();
                var blockNumber = BigInteger.Parse(request.BlockNumber);

                var blockData = await _blockProvider.GetBlockAsync<object>(
                    feedRuntime.ChainId,
                    dataType,
                    blockNumber,
                    requestCt);

                if (blockData == null)
                {
                    var dataSubject = FeedSubjects.System.TestData(feedRuntime.ChainId);
                    var dataRequest = new FeedDataRequest(request.DeployRequest.FeedDataType, request.BlockNumber);

                    var dataResponse = await _requestClient.SendAsync<FeedDataRequest, FeedDataResponse>(
                        dataSubject,
                        dataRequest,
                        _requestTimeout,
                        requestCt);

                    blockData = dataResponse?.Data;

                    if (blockData == null)
                    {
                        var filterError = new FilterErrorData(
                            Message: dataResponse?.Error ?? "Failed to retrieve test data");

                        resp = new FeedTestResponse(FilterError: filterError);
                    }
                }

                if (blockData != null)
                {
                    var result = await _feedManager.ExecuteTestAsync(feedRuntime, blockData, ct: requestCt);

                    resp = new FeedTestResponse(
                        FilterResult: result.FilterResult,
                        FunctionResult: result.FunctionResult);

                    if (request.ExecuteOutputs == true)
                    {
                        var dataToSend = result.FunctionResult ?? result.FilterResult ?? blockData;

                        var deliveryResp = await _requestClient.SendAsync<DeliverTestOutputRequest, DeliverTestOutputResponse>(
                            FeedSubjects.System.DeliverTestOutput,
                            new DeliverTestOutputRequest(feedRuntime.Id, feedRuntime.OutputIds, dataToSend),
                            _requestTimeout,
                            requestCt);

                        if (deliveryResp?.Success != true)
                        {
                            _logger.LogWarning(
                                "Failed to deliver test output for {FeedId}: {Error}",
                                feedRuntime.Id,
                                deliveryResp?.Error ?? "No response");
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Test request {FeedId} timed out after {Seconds}s",
                feedId,
                _requestTimeout.TotalSeconds);

            resp = new FeedTestResponse(
                ServerError: $"Test execution timed out after {_requestTimeout.TotalSeconds}s");
        }
        catch (FeedEngineException ex)
        {
            var errorData = new FilterErrorData(ex.Message, ex.Line, ex.Column);

            resp = new FeedTestResponse(
                FilterError: !ex.IsFunctionError ? errorData : null,
                FunctionError: ex.IsFunctionError ? errorData : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test request {FeedId} failed unexpectedly", feedId);
            resp = new FeedTestResponse(ServerError: $"$Internal server error: {ex.Message}");
        }

        if (resp != null && !string.IsNullOrEmpty(replyTo))
        {
            try
            {
                await _serviceBus.PublishAsync(replyTo, resp, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to publish test response to {ReplyTo} for {FeedId}",
                    replyTo,
                    feedId);
            }
        }

        _logger.LogInformation(
            "Test request {FeedId} completed in {ElapsedMs}ms (success={Success})",
            feedId,
            sw.ElapsedMilliseconds,
            resp?.ServerError == null);
    }
}
