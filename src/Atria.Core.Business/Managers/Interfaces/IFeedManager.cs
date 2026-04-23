using Atria.Common.Models.Generic;
using Atria.Core.Business.Models.Dto.Feed;
using Atria.Core.Data.Models.Query;

namespace Atria.Core.Business.Managers.Interfaces;

public interface IFeedManager : IBaseManager
{
    Task<FeedDto> CreateFeedAsync(CreateFeedDto dto, CancellationToken ct);

    Task<FeedDto> UpdateFeedAsync(Guid id, UpdateFeedDto dto, CancellationToken ct);

    Task<FeedDto> GetFeedAsync(Guid id, CancellationToken ct);

    Task<PagedList<FeedDto>> GetFeedsAsync(QueryOptions<FeedDto> queryOptions, CancellationToken ct);

    Task DeleteFeedAsync(Guid id, CancellationToken ct);

    Task StartFeedAsync(Guid id, bool resetCursor, CancellationToken ct);

    Task PauseFeedAsync(Guid id, CancellationToken ct);

    Task<TestResultDto> TestFeedAsync(TestRequestDto dto, CancellationToken ct);

    Task<List<ResultDto>> GetResultsByFeedIdAsync(Guid feedId, int limit, CancellationToken ct);

    IAsyncEnumerable<ResultDto> StreamResultsByFeedIdAsync(Guid feedId, ulong? afterSeq, CancellationToken ct);

    IAsyncEnumerable<IReadOnlyList<StatusDto>> StreamStatusesByChainAsync(string chainId, IEnumerable<Guid> feedIds, CancellationToken ct);
}
