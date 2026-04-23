using Atria.Common.Models.Generic;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Models.Query;

namespace Atria.Core.Data.Repositories.Context.Interfaces;

public interface IFeedRepository : IRepository<Guid, Feed>
{
    Task<PagedList<Feed>> GetFeedsAsync(
        QueryOptions<Feed>? queryOptions,
        CancellationToken ct);
}
