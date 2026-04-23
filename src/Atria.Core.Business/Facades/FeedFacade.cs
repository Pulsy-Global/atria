using Atria.Common.Models.Generic;
using Atria.Common.Web.OData.Models;
using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Dto.Feed;
using Atria.Core.Business.Models.Dto.Output;
using Atria.Core.Data.Extensions;

namespace Atria.Core.Business.Facades;

public class FeedFacade(IFeedManager feedManager, IDeployManager deployManager, IOutputManager outputManager)
{
    public async Task<FeedDto> CreateFeedAsync(CreateFeedDto dto, CancellationToken ct) =>
        await feedManager.CreateFeedAsync(dto, ct);

    public async Task<List<DeployDto>> GetDeploysByFeedIdAsync(Guid id, CancellationToken ct) =>
        await deployManager.GetDeploysByFeedIdAsync(id, ct);

    public async Task<List<OutputDto>> GetOutputsByFeedIdAsync(Guid feedId, CancellationToken ct) =>
        await outputManager.GetOutputsByFeedIdAsync(feedId, ct);

    public async Task<List<ResultDto>> GetResultsByFeedIdAsync(Guid feedId, int limit, CancellationToken ct) =>
        await feedManager.GetResultsByFeedIdAsync(feedId, limit, ct);

    public IAsyncEnumerable<ResultDto> StreamResultsByFeedIdAsync(Guid feedId, ulong? afterSeq, CancellationToken ct) =>
        feedManager.StreamResultsByFeedIdAsync(feedId, afterSeq, ct);

    public IAsyncEnumerable<IEnumerable<StatusDto>> StreamStatusesByChainAsync(string chainId, IEnumerable<Guid> feedIds, CancellationToken ct) =>
        feedManager.StreamStatusesByChainAsync(chainId, feedIds, ct);

    public async Task<FeedDto> UpdateFeedAsync(Guid id, UpdateFeedDto dto, CancellationToken ct) =>
        await feedManager.UpdateFeedAsync(id, dto, ct);

    public async Task<FeedDto> GetFeedAsync(Guid id, CancellationToken ct) =>
        await feedManager.GetFeedAsync(id, ct);

    public async Task<PagedList<FeedDto>> GetFeedsAsync(ODataQueryParams<FeedDto> queryParams, CancellationToken ct) =>
        await feedManager.GetFeedsAsync(queryParams.ToQueryOptions(), ct);

    public async Task StartFeedAsync(Guid id, bool resetCursor, CancellationToken ct) =>
        await feedManager.StartFeedAsync(id, resetCursor, ct);

    public async Task PauseFeedAsync(Guid id, CancellationToken ct) =>
        await feedManager.PauseFeedAsync(id, ct);

    public async Task DeleteFeedAsync(Guid id, CancellationToken ct) =>
        await feedManager.DeleteFeedAsync(id, ct);

    public async Task<TestResultDto> TestFeedAsync(TestRequestDto dto, CancellationToken ct) =>
        await feedManager.TestFeedAsync(dto, ct);
}
