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
    private const int DeployHistoryRetentionLimit = 20;

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
            _logger.LogError(
                "Feed cursor (block {FeedCursor}) is behind chain tail (block {ChainTail}).",
                feedCursor,
                tail);

            throw new CursorBehindTailException(feedCursor.Value, tail.Value);
        }

        await DeactivatePreviousDeploysAsync(uow, feedId, ct);

        var deploy = await uow.DeployRepository.CreateAsync(
            new Deploy
            {
                Id = Guid.CreateVersion7(),
                FeedId = feedId,
                Version = feed.Version,
                Status = DeployStatus.Pending,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
            ct);

        feed.Status = FeedStatus.Pending;
        feed.CurrentDeployId = deploy.Id;
        uow.FeedRepository.Update(feed);

        await uow.SaveChangesAsync(ct);
        await TrimDeployHistoryAsync(uow, feedId, deploy.Id, ct);

        try
        {
            await SendDeployRequestAsync(feed, deploy.Id, ct);

            return deploy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy feed: {FeedId}", feedId);

            deploy.MarkFailed(
                DeployErrorCode.DeploymentFailed,
                nameof(DeployErrorCode.DeploymentFailed),
                "The deployment request could not be published.");
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

        var deployId = feed.CurrentDeployId;

        if (!deployId.HasValue)
        {
            var deploy = await uow.DeployRepository.GetAsync(
                x => x.FeedId == feedId
                    && (x.Status == DeployStatus.Pending || x.Status == DeployStatus.Deployed),
                ct);

            deployId = deploy?.Id;
        }

        if (!deployId.HasValue)
        {
            throw new InvalidOperationException($"Current deploy for feed with ID {feedId} not found");
        }

        await SendDeployRequestAsync(feed, deployId.Value, ct);
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
            await _feedEventPublisher.PublishFeedPauseAsync(feedId, entity.CurrentDeployId, ct);

            entity.Status = FeedStatus.Paused;
            await MarkCurrentDeployStoppedAsync(uow, entity, ct);

            await uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause feed: {FeedId}", feedId);

            entity.Status = FeedStatus.Error;

            await MarkCurrentDeployFailedAsync(
                uow,
                entity,
                DeployErrorCode.OperationFailed,
                "Pause",
                "The feed could not be paused.",
                ct);

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
            await MarkCurrentDeployStoppedAsync(uow, entity, ct);

            await uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete feed: {FeedId}", feedId);

            entity.Status = FeedStatus.Error;
            await MarkCurrentDeployFailedAsync(
                uow,
                entity,
                DeployErrorCode.OperationFailed,
                "Delete",
                "The feed could not be deleted from runtime.",
                ct);

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

        if (feed.CurrentDeployId.HasValue)
        {
            return await uow.DeployRepository.GetAsync(feed.CurrentDeployId.Value, ct);
        }

        return await uow.DeployRepository.GetAsync(
            x => x.FeedId == feedId
                && (x.Status == DeployStatus.Pending || x.Status == DeployStatus.Deployed),
            ct);
    }

    public async Task<bool> ConfirmDeployedAsync(Guid feedId, Guid deployId, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var feed = await uow.FeedRepository.GetAsync(feedId, ct);

        if (feed == null)
        {
            throw new InvalidOperationException($"Feed with ID {feedId} not found");
        }

        if (feed.CurrentDeployId.HasValue && feed.CurrentDeployId.Value != deployId)
        {
            _logger.LogInformation(
                "Ignoring stale deployed event for feed {FeedId}: event deploy {DeployId}, current deploy {CurrentDeployId}",
                feedId,
                deployId,
                feed.CurrentDeployId.Value);

            return false;
        }

        var deploy = await uow.DeployRepository.GetAsync(deployId, ct);

        if (deploy == null || deploy.FeedId != feedId)
        {
            _logger.LogWarning(
                "Ignoring deployed event for feed {FeedId}: deploy {DeployId} was not found or belongs to another feed",
                feedId,
                deployId);

            return false;
        }

        if (deploy.Status != DeployStatus.Pending && deploy.Status != DeployStatus.Deployed)
        {
            _logger.LogInformation(
                "Ignoring deployed event for feed {FeedId}: deploy {DeployId} is in status {Status}",
                feedId,
                deployId,
                deploy.Status);

            return false;
        }

        deploy.Status = DeployStatus.Deployed;
        deploy.ClearError();
        deploy.UpdatedAt = DateTimeOffset.UtcNow;
        uow.DeployRepository.Update(deploy);
        feed.CurrentDeployId = deploy.Id;

        feed.Status = FeedStatus.Running;
        uow.FeedRepository.Update(feed);

        await uow.SaveChangesAsync(ct);

        return true;
    }

    public async Task<bool> FailCurrentDeploymentAsync(
        Guid feedId,
        Guid? expectedDeployId,
        DeployErrorCode errorCode,
        string source,
        string? message,
        CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var feed = await uow.FeedRepository.GetAsync(feedId, ct);

        if (feed == null)
        {
            throw new InvalidOperationException($"Feed with ID {feedId} not found");
        }

        if (feed.Status != FeedStatus.Running || feed.CurrentDeployId != expectedDeployId)
        {
            _logger.LogInformation(
                "Ignoring stale deployment failure for feed {FeedId}: expected deploy {ExpectedDeployId}, current deploy {CurrentDeployId}, status {FeedStatus}",
                feedId,
                expectedDeployId,
                feed.CurrentDeployId,
                feed.Status);

            return false;
        }

        feed.Status = FeedStatus.Error;
        uow.FeedRepository.Update(feed);

        await MarkCurrentDeployFailedAsync(
            uow,
            feed,
            errorCode,
            source,
            message,
            ct);

        await uow.SaveChangesAsync(ct);

        return true;
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

    private async Task DeactivatePreviousDeploysAsync(IUnitOfWork uow, Guid feedId, CancellationToken ct)
    {
        var activeDeploys = await uow.DeployRepository.GetListAsync(
            x => x.FeedId == feedId
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

    private async Task TrimDeployHistoryAsync(
        IUnitOfWork uow,
        Guid feedId,
        Guid currentDeployId,
        CancellationToken ct)
    {
        try
        {
            var deploys = await uow.DeployRepository.GetListAsync(
                x => x.FeedId == feedId,
                ct);

            if (deploys.Count <= DeployHistoryRetentionLimit)
            {
                return;
            }

            var keepIds = deploys
                .OrderByDescending(x => x.CreatedAt)
                .Take(DeployHistoryRetentionLimit)
                .Select(x => x.Id)
                .Append(currentDeployId)
                .ToHashSet();

            var deleteIds = deploys
                .Where(x => !keepIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToList();

            if (deleteIds.Count == 0)
            {
                return;
            }

            await uow.DeployRepository.ExecuteDeleteAsync(
                x => deleteIds.Contains(x.Id),
                ct);

            _logger.LogInformation(
                "Trimmed {DeployCount} old deploy history records for feed {FeedId}",
                deleteIds.Count,
                feedId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to trim old deploy history records for feed {FeedId}",
                feedId);
        }
    }

    private async Task MarkCurrentDeployStoppedAsync(IUnitOfWork uow, Feed feed, CancellationToken ct)
    {
        if (!feed.CurrentDeployId.HasValue)
        {
            return;
        }

        var deploy = await uow.DeployRepository.GetAsync(feed.CurrentDeployId.Value, ct);

        if (deploy == null)
        {
            return;
        }

        deploy.Status = DeployStatus.None;
        deploy.UpdatedAt = DateTimeOffset.UtcNow;
        deploy.ClearError();
        uow.DeployRepository.Update(deploy);
    }

    private async Task MarkCurrentDeployFailedAsync(
        IUnitOfWork uow,
        Feed feed,
        DeployErrorCode errorCode,
        string source,
        string? message,
        CancellationToken ct)
    {
        if (!feed.CurrentDeployId.HasValue)
        {
            return;
        }

        var deploy = await uow.DeployRepository.GetAsync(feed.CurrentDeployId.Value, ct);

        if (deploy == null)
        {
            return;
        }

        deploy.MarkFailed(errorCode, source, message);
        uow.DeployRepository.Update(deploy);
    }

    private async Task SendDeployRequestAsync(Feed feed, Guid deployId, CancellationToken ct)
    {
        var (filterCode, functionCode) = await GetFeedCode(feed, ct);
        var resourceNamespace = await _resourceNamespaceResolver.ResolveForFeedAsync(feed.Id, ct);

        var req = new FeedDeployRequest(
            Id: feed.Id.ToString(),
            DeployId: deployId.ToString(),
            ChainId: feed.NetworkId,
            FilterCode: filterCode,
            FunctionCode: functionCode,
            OutputIds: feed.FeedOutputs.Select(x => x.OutputId.ToString()).ToList(),
            FeedDataType: ConvertAtriaDataTypeToFeedDataType(feed.DataType),
            Type: string.IsNullOrEmpty(filterCode) ? FeedType.Passthrough : FeedType.Filtered,
            BlockDelay: feed.BlockDelay,
            ErrorHandling: (Contracts.Events.Feed.Enums.ErrorHandlingStrategy)(int)feed.ErrorHandling,
            EkvNamespace: resourceNamespace,
            ResourceNamespace: resourceNamespace);

        await _feedEventPublisher.PublishFeedDeployAsync(req, ct);
    }
}
