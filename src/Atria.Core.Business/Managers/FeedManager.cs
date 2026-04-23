using Atria.Business.Models;
using Atria.Business.Models.Enums;
using Atria.Business.Services.DataServices.Interfaces;
using Atria.Business.Services.Messaging.Interfaces;
using Atria.Common.Exceptions;
using Atria.Common.Models.Generic;
using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Dto.Feed;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Extensions;
using Atria.Core.Data.Models.Query;
using Atria.Pipeline.Interfaces;
using Atria.Pipeline.Stores;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Atria.Core.Business.Managers;

public class FeedManager(
    IFeedDataService feedDataService,
    IFeedMessageService feedMessageService,
    IFeedCursorStore feedCursorStore,
    IDeployDataService deployDataService,
    ChainStateStore chainStateStore,
    ILogger<FeedManager> logger,
    IMapper mapper)
    : BaseManager(logger, mapper), IFeedManager
{
    public async Task<FeedDto> CreateFeedAsync(CreateFeedDto dto, CancellationToken ct)
    {
        var entity = Mapper.Map<Feed>(dto);

        if (!string.IsNullOrEmpty(dto.FilterCode))
        {
            var path = await feedDataService.UploadFeedFileAsync(entity.Id, FeedFileType.Filter, dto.FilterCode, ct);
            entity.FilterPath = path;
        }

        if (!string.IsNullOrEmpty(dto.FunctionCode))
        {
            var path = await feedDataService.UploadFeedFileAsync(entity.Id, FeedFileType.Function, dto.FunctionCode, ct);
            entity.FunctionPath = path;
        }

        var createdEntity = await feedDataService.CreateFeedAsync(entity, ct, dto.OutputIds, dto.TagIds);

        return Mapper.Map<FeedDto>(createdEntity);
    }

    public async Task<FeedDto> UpdateFeedAsync(Guid id, UpdateFeedDto dto, CancellationToken ct)
    {
        var entity = await feedDataService.GetFeedByIdAsync(id, ct, x => x.FeedOutputs, x => x.FeedTags);

        var previousNetworkId = entity.NetworkId;

        Mapper.Map(dto, entity);

        if (!string.IsNullOrEmpty(dto.FilterCode))
        {
            var path = await feedDataService.UploadFeedFileAsync(entity.Id, FeedFileType.Filter, dto.FilterCode, ct);
            entity.FilterPath = path;
        }
        else if (!string.IsNullOrEmpty(entity.FilterPath))
        {
            await feedDataService.DeleteFeedFileAsync(id, FeedFileType.Filter, ct);
            entity.FilterPath = null;
        }

        if (!string.IsNullOrEmpty(dto.FunctionCode))
        {
            var path = await feedDataService.UploadFeedFileAsync(entity.Id, FeedFileType.Function, dto.FunctionCode, ct);
            entity.FunctionPath = path;
        }
        else if (!string.IsNullOrEmpty(entity.FunctionPath))
        {
            await feedDataService.DeleteFeedFileAsync(id, FeedFileType.Function, ct);
            entity.FunctionPath = null;
        }

        var networkChanged = !string.Equals(previousNetworkId, entity.NetworkId, StringComparison.Ordinal);

        if (networkChanged)
        {
            await feedCursorStore.DeleteAsync(entity.Id.ToString(), ct);
        }

        var updatedEntity = await feedDataService.UpdateFeedAsync(entity, ct, dto.OutputIds, dto.TagIds);

        if (entity.Status is FeedStatus.Running or FeedStatus.Pending)
        {
            await deployDataService.PauseFromRuntimeAsync(entity.Id, ct);
            await deployDataService.ExecuteDeploymentAsync(entity.Id, ct);
        }

        var result = Mapper.Map<FeedDto>(updatedEntity);

        result.FilterCode = await feedDataService.GetFeedFileAsync(id, FeedFileType.Filter, ct);
        result.FunctionCode = await feedDataService.GetFeedFileAsync(id, FeedFileType.Function, ct);

        return result;
    }

    public async Task<TestResultDto> TestFeedAsync(TestRequestDto dto, CancellationToken ct)
    {
        var request = Mapper.Map<TestRequest>(dto);

        var result = await deployDataService.TestFeedDeployAsync(request, ct);

        return Mapper.Map<TestResultDto>(result);
    }

    public async Task<FeedDto> GetFeedAsync(Guid id, CancellationToken ct)
    {
        var entity = await feedDataService.GetFeedByIdAsync(id, ct, x => x.FeedTags, x => x.FeedOutputs);

        var dto = Mapper.Map<FeedDto>(entity);

        dto.FilterCode = await feedDataService.GetFeedFileAsync(id, FeedFileType.Filter, ct);
        dto.FunctionCode = await feedDataService.GetFeedFileAsync(id, FeedFileType.Function, ct);

        return dto;
    }

    public async Task DeleteFeedAsync(Guid id, CancellationToken ct)
    {
        await deployDataService.DeleteFromRuntimeAsync(id, ct);

        await feedDataService.DeleteFeedAsync(id, ct);
    }

    public async Task<PagedList<FeedDto>> GetFeedsAsync(QueryOptions<FeedDto> queryOptions, CancellationToken ct)
    {
        var mappedQuery = Mapper.MapQueryOptions<FeedDto, Feed>(queryOptions);

        var entities = await feedDataService.GetFeedsAsync(mappedQuery, ct);

        return Mapper.Map<PagedList<FeedDto>>(entities);
    }

    public async Task StartFeedAsync(Guid id, bool resetCursor, CancellationToken ct)
    {
        var entity = await feedDataService.GetFeedByIdAsync(id, ct)
            ?? throw new ItemNotFoundException($"Feed with id {id} not found");

        if (resetCursor)
        {
            await feedCursorStore.DeleteAsync(entity.Id.ToString(), ct);
        }

        await deployDataService.ExecuteDeploymentAsync(entity.Id, ct);
    }

    public async Task PauseFeedAsync(Guid id, CancellationToken ct)
    {
        var entity = await feedDataService.GetFeedByIdAsync(id, ct)
            ?? throw new ItemNotFoundException($"Feed with id {id} not found");

        await deployDataService.PauseFromRuntimeAsync(entity.Id, ct);
    }

    public async Task<List<ResultDto>> GetResultsByFeedIdAsync(Guid feedId, int limit, CancellationToken ct)
    {
        var messages = await feedMessageService.GetFeedOutputsAsync(feedId, limit, ct);
        return Mapper.Map<List<ResultDto>>(messages);
    }

    public async IAsyncEnumerable<ResultDto> StreamResultsByFeedIdAsync(
        Guid feedId,
        ulong? afterSeq,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var msg in feedMessageService.StreamFeedOutputsAsync(feedId, afterSeq, ct))
        {
            yield return Mapper.Map<ResultDto>(msg);
        }
    }

    public async IAsyncEnumerable<IReadOnlyList<StatusDto>> StreamStatusesByChainAsync(
        string chainId,
        IEnumerable<Guid> feedIds,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var feeds = feedIds as Guid[] ?? feedIds.ToArray();

        await foreach (var head in chainStateStore.StreamForHeadAsync(chainId, ct))
        {
            var results = new List<StatusDto>();

            foreach (var feedId in feeds)
            {
                var cursor = await feedCursorStore.GetAsync(feedId.ToString(), ct);

                if (cursor != null)
                {
                    var feedCursor = (ulong)cursor.Value;
                    var chainHead = (ulong)head;

                    results.Add(new StatusDto
                    {
                        FeedId = feedId,
                        FeedCursor = feedCursor,
                        ChainHead = Math.Max(feedCursor, chainHead),
                    });
                }
            }

            yield return results;
        }
    }
}
