using Atria.Common.Models.Generic;
using Atria.Core.Data.Context;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Extensions;
using Atria.Core.Data.Models.Query;
using Atria.Core.Data.Repositories.Context.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Atria.Core.Data.Repositories.Context;

public class FeedRepository : Repository<Guid, Feed>, IFeedRepository
{
    public FeedRepository(AtriaDbContext context)
        : base(context)
    {
    }

    public async Task<PagedList<Feed>> GetFeedsAsync(
        QueryOptions<Feed>? queryOptions,
        CancellationToken ct)
    {
        var query = GetSet()
            .Include(x => x.FeedTags)
            .ThenInclude(x => x.Tag)
            .AsQueryable();
        var options = queryOptions ?? new QueryOptions<Feed>();

        if (!string.IsNullOrWhiteSpace(options.SearchQuery))
        {
            query = query.ApplyFeedSearch(options.SearchQuery);
        }
        else if (options.OrderByOptions == null)
        {
            query = query.OrderByDescending(x => x.CreatedAt);
        }

        var items = await query.ApplyQueryOptions(options).ToListAsync(ct);

        var countQuery = GetSet().AsQueryable();
        if (!string.IsNullOrWhiteSpace(options.SearchQuery))
        {
            countQuery = countQuery.ApplyFeedSearch(options.SearchQuery);
        }

        var count = await countQuery.Filter(options).CountAsync(ct);

        return new PagedList<Feed>
        {
            Items = items,
            TotalCount = count,
        };
    }
}
