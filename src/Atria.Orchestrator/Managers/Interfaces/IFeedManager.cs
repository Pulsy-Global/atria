using Atria.Common.Models.Generic;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Models.Query;

namespace Atria.Orchestrator.Managers.Interfaces;

public interface IFeedManager : IBaseManager
{
    Task<Feed> CreateFeedAsync(Feed dto, CancellationToken ct);

    Task<Feed> UpdateFeedAsync(Guid id, Feed feed, CancellationToken ct);

    Task<Feed> GetFeedAsync(Guid id, CancellationToken ct);

    Task<PagedList<Feed>> GetFeedsAsync(QueryOptions<Feed> queryOptions, CancellationToken ct);

    Task DeleteFeedAsync(Guid id, CancellationToken ct);
}
