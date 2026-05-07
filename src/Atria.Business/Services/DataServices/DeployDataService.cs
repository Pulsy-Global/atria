using Atria.Business.Models;
using Atria.Business.Services.DataServices.Interfaces;
using Atria.Business.Services.Deployment.Interfaces;
using Atria.Business.Services.Namespaces.Interfaces;
using Atria.Business.Services.Storage.Interfaces;
using Atria.Common.Exceptions;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;
using Atria.Core.Data.Entities.Deploys;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.UnitOfWork.Context;
using Atria.Core.Data.UnitOfWork.Factory;
using Atria.Pipeline.Interfaces;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Atria.Business.Services.DataServices;

public class DeployDataService : IDeployDataService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly ILogger<DeployDataService> _logger;
    private readonly IFeedEventPublisher _feedEventPublisher;
    private readonly IFeedCursorStore _feedCursorStore;
    private readonly ChainStateStore _chainStateStore;
    private readonly IFileSystemService _fileStorageService;
    private readonly IResourceNamespaceResolver _resourceNamespaceResolver;

    public DeployDataService(
        IFeedEventPublisher feedEventPublisher,
        IFeedCursorStore feedCursorStore,
        ChainStateStore chainStateStore,
        IUnitOfWorkFactory unitOfWorkFactory,
        IFileSystemService fileStorageService,
        IResourceNamespaceResolver resourceNamespaceResolver,
        ILogger<DeployDataService> logger)
    {
        _feedEventPublisher = feedEventPublisher;
        _feedCursorStore = feedCursorStore;
        _chainStateStore = chainStateStore;
        _unitOfWorkFactory = unitOfWorkFactory;
        _fileStorageService = fileStorageService;
        _resourceNamespaceResolver = resourceNamespaceResolver;
        _logger = logger;
    }

    public async Task<List<Deploy>> GetDeploysAsync(Expression<Func<Deploy, bool>> predicate, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        return await uow.DeployRepository.GetListAsync(predicate, ct);
    }

    public async Task<Deploy> ExecuteDeploymentAsync(Guid feedId, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var feed = await uow.FeedRepository.GetAsync(
            x => x.Id == feedId, ct, x => x.FeedOutputs);

        if (feed == null)
        {
            throw new InvalidOperationException($"Feed with ID {feedId} not found");
        }

        var feedCursor = await _feedCursorStore.GetAsync(feed.Id.ToString(), ct);
        var tail = await _chainStateStore.GetTailAsync(feed.NetworkId, ct);

        if (feedCursor.HasValue && feedCursor < tail)
        {
            _logger.LogError("Feed cursor (block {}) is behind chain tail (block {}).", feedCursor, tail);
            throw new CursorBehindTailException(feedCursor.Value, tail.Value);
        }

        await DeactivatePreviousDeploysAsync(uow, feedId, feed.Version, ct);

        var deploy = await uow.DeployRepository.GetAsync(
            x => x.FeedId == feedId && x.Version == feed.Version, ct);

        if (deploy == null)
        {
            deploy = await uow.DeployRepository.CreateAsync(
                new Deploy
                {
                    FeedId = feedId,
                    Version = feed.Version,
                    Status = DeployStatus.Pending,
                    UpdatedAt = DateTimeOffset.UtcNow,
                },
                ct);
        }
        else
        {
            deploy.Status = DeployStatus.Pending;
            deploy.UpdatedAt = DateTimeOffset.UtcNow;
            uow.DeployRepository.Update(deploy);
        }

        feed.Status = FeedStatus.Pending;
        uow.FeedRepository.Update(feed);

        await uow.SaveChangesAsync(ct);

        try
        {
            await SendDeployRequestAsync(feed, ct);

            return deploy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy feed: {FeedId}", feedId);

            deploy.Status = DeployStatus.Failed;
            feed.Status = FeedStatus.Error;

            uow.FeedRepository.Update(feed);
            uow.DeployRepository.Update(deploy);

            await uow.SaveChangesAsync(ct);

            throw;
        }
    }

    public async Task PublishDeployRequestAsync(Guid feedId, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var feed = await uow.FeedRepository.GetAsync(
            x => x.Id == feedId, ct, x => x.FeedOutputs);

        if (feed == null)
        {
            throw new InvalidOperationException($"Feed with ID {feedId} not found");
        }

        await SendDeployRequestAsync(feed, ct);
    }

    public async Task PauseFromRuntimeAsync(Guid feedId, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entity = await uow.FeedRepository.GetAsync(feedId, ct);

        if (entity == null)
        {
            throw new InvalidOperationException($"Feed with ID {feedId} not found");
        }

        entity.Status = FeedStatus.Pending;

        uow.FeedRepository.Update(entity);

        await uow.SaveChangesAsync(ct);

        try
        {
            await _feedEventPublisher.PublishFeedPauseAsync(feedId, ct);

            entity.Status = FeedStatus.Paused;

            await uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause feed: {FeedId}", feedId);

            entity.Status = FeedStatus.Error;

            await uow.SaveChangesAsync(ct);

            throw;
        }
    }

    public async Task DeleteFromRuntimeAsync(Guid feedId, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entity = await uow.FeedRepository.GetAsync(feedId, ct);

        if (entity == null)
        {
            throw new InvalidOperationException($"Feed with ID {feedId} not found");
        }

        entity.Status = FeedStatus.Pending;

        uow.FeedRepository.Update(entity);

        await uow.SaveChangesAsync(ct);

        try
        {
            await _feedEventPublisher.PublishFeedDeleteAsync(feedId, ct);

            entity.Status = FeedStatus.Paused;

            await uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete feed: {FeedId}", feedId);

            entity.Status = FeedStatus.Error;

            await uow.SaveChangesAsync(ct);

            throw;
        }
    }

    public async Task<Deploy> UpdateDeployAsync(Deploy deploy, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        uow.DeployRepository.Update(deploy);

        await uow.SaveChangesAsync(ct);

        return deploy;
    }

    public async Task<Deploy?> GetCurrentDeployAsync(Guid feedId, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var feed = await uow.FeedRepository.GetAsync(feedId, ct);

        if (feed == null)
        {
            return null;
        }

        return await uow.DeployRepository.GetAsync(
            x => x.FeedId == feedId && x.Version == feed.Version,
            ct);
    }

    public async Task ConfirmDeployedAsync(Guid feedId, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var feed = await uow.FeedRepository.GetAsync(feedId, ct);

        if (feed == null)
        {
            throw new InvalidOperationException($"Feed with ID {feedId} not found");
        }

        if (feed.Status == FeedStatus.Running)
        {
            return;
        }

        var deploy = await uow.DeployRepository.GetAsync(
            x => x.FeedId == feedId && x.Version == feed.Version,
            ct);

        if (deploy != null)
        {
            deploy.Status = DeployStatus.Deployed;
            deploy.UpdatedAt = DateTimeOffset.UtcNow;
            uow.DeployRepository.Update(deploy);
        }

        feed.Status = FeedStatus.Running;
        uow.FeedRepository.Update(feed);

        await uow.SaveChangesAsync(ct);
    }

    public async Task<TestResult> TestFeedDeployAsync(TestRequest request, CancellationToken ct)
    {
        var feedDataType = ConvertAtriaDataTypeToFeedDataType(request.DataType);

        var result = await _feedEventPublisher.ExecuteFeedTestAsync(request, feedDataType, ct);

        if (result.ServerError != null)
        {
            throw new ApplicationException($"Server error during feed test: {result.ServerError}");
        }

        return result;
    }

    private async Task<Tuple<string, string>> GetFeedCode(Feed feed, CancellationToken ct)
    {
        string filterCode = string.Empty;
        if (feed.FilterPath != null)
        {
            filterCode = await ReadCodeToString(feed.FilterPath, ct);
        }

        string functionCode = string.Empty;
        if (feed.FunctionPath != null)
        {
            functionCode = await ReadCodeToString(feed.FunctionPath, ct);
        }

        return new Tuple<string, string>(filterCode, functionCode);
    }

    private async Task<string> ReadCodeToString(string fullPath, CancellationToken ct)
    {
        await using var stream = await _fileStorageService.GetFileAsync(fullPath, ct);

        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync(ct);
    }

    private FeedDataType ConvertAtriaDataTypeToFeedDataType(AtriaDataType dataType) =>
        dataType switch
        {
            AtriaDataType.BlockWithLogs => FeedDataType.Logs,
            AtriaDataType.BlockWithTraces => FeedDataType.Traces,
            AtriaDataType.BlockWithTransactions => FeedDataType.Transactions,
            _ => throw new ArgumentOutOfRangeException(nameof(dataType), $"Unknown data type: {dataType}")
        };

    private async Task DeactivatePreviousDeploysAsync(IUnitOfWork uow, Guid feedId, string currentVersion, CancellationToken ct)
    {
        var activeDeploys = await uow.DeployRepository.GetListAsync(
            x => x.FeedId == feedId
                && x.Version != currentVersion
                && (x.Status == DeployStatus.Deployed || x.Status == DeployStatus.Pending),
            ct);

        foreach (var deploy in activeDeploys)
        {
            deploy.Status = DeployStatus.None;
            deploy.UpdatedAt = DateTimeOffset.UtcNow;
            uow.DeployRepository.Update(deploy);
        }

        if (activeDeploys.Count > 0)
        {
            await uow.SaveChangesAsync(ct);
        }
    }

    private async Task SendDeployRequestAsync(Feed feed, CancellationToken ct)
    {
        var (filterCode, functionCode) = await GetFeedCode(feed, ct);

        var req = new FeedDeployRequest(
            Id: feed.Id.ToString(),
            ChainId: feed.NetworkId,
            FilterCode: filterCode,
            FunctionCode: functionCode,
            OutputIds: feed.FeedOutputs.Select(x => x.OutputId.ToString()).ToList(),
            FeedDataType: ConvertAtriaDataTypeToFeedDataType(feed.DataType),
            Type: string.IsNullOrEmpty(filterCode) ? FeedType.Passthrough : FeedType.Filtered,
            BlockDelay: feed.BlockDelay,
            ErrorHandling: (Contracts.Events.Feed.Enums.ErrorHandlingStrategy)(int)feed.ErrorHandling,
            EkvNamespace: await _resourceNamespaceResolver.ResolveForFeedAsync(feed.Id, ct));

        await _feedEventPublisher.PublishFeedDeployAsync(req, ct);
    }
}
