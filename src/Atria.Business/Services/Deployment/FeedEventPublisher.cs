using Atria.Business.Models;
using Atria.Business.Services.Deployment.Interfaces;
using Atria.Business.Services.Namespaces.Interfaces;
using Atria.Common.Messaging.RequestReply;
using Atria.Common.Messaging.ServiceBus;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;
using Atria.Contracts.Subjects.Feed;

namespace Atria.Business.Services.Deployment;

public class FeedEventPublisher : IFeedEventPublisher
{
    private readonly IServiceBus _serviceBus;
    private readonly IRequestClient _requestClient;
    private readonly IResourceNamespaceResolver _resourceNamespaceResolver;

    public FeedEventPublisher(IServiceBus serviceBus, IRequestClient requestClient, IResourceNamespaceResolver resourceNamespaceResolver)
    {
        _serviceBus = serviceBus;
        _requestClient = requestClient;
        _resourceNamespaceResolver = resourceNamespaceResolver;
    }

    public async Task<TestResult> ExecuteFeedTestAsync(TestRequest request, FeedDataType dataType, CancellationToken ct = default)
    {
        var deployRequest = new FeedDeployRequest(
            Id: Guid.NewGuid().ToString(),
            ChainId: request.BlockchainId,
            FeedDataType: dataType,
            FilterCode: request.FilterCode,
            FunctionCode: request.FunctionCode,
            OutputIds: request.OutputsIds,
            Type: string.IsNullOrEmpty(request.FilterCode) ? FeedType.Passthrough : FeedType.Filtered,
            EkvNamespace: _resourceNamespaceResolver.Resolve());

        var req = new FeedTestRequest(
            DeployRequest: deployRequest,
            BlockNumber: request.BlockNumber,
            ExecuteOutputs: request.ExecuteOutputs);

        var response = await _requestClient.SendAsync<FeedTestRequest, FeedTestResponse>(
            FeedSubjects.System.TestRequest, req, ct: ct);

        return new TestResult
        {
            FilterResult = response?.FilterResult,
            FunctionResult = response?.FunctionResult,
            ServerError = response?.ServerError,
            FilterError = response?.FilterError == null ? null : new ExecutionError
            {
                Message = response.FilterError.Message,
                Line = response.FilterError.Line,
                Column = response.FilterError.Column,
            },
            FunctionError = response?.FunctionError == null ? null : new ExecutionError
            {
                Message = response.FunctionError.Message,
                Line = response.FunctionError.Line,
                Column = response.FunctionError.Column,
            },
        };
    }

    public async Task PublishFeedDeployAsync(FeedDeployRequest request, CancellationToken ct = default)
    {
        await _serviceBus.PublishAsync(FeedSubjects.System.DeployRequest, request, ct);
    }

    public async Task PublishFeedPauseAsync(Guid feedId, CancellationToken ct = default)
    {
        var req = new FeedPauseRequest(Id: feedId.ToString());
        await _serviceBus.PublishAsync(FeedSubjects.System.PauseRequest, req, ct);
    }

    public async Task PublishFeedDeleteAsync(Guid feedId, CancellationToken ct = default)
    {
        var req = new FeedDeleteRequest(Id: feedId.ToString());
        await _serviceBus.PublishAsync(FeedSubjects.System.DeleteRequest, req, ct);
    }
}
