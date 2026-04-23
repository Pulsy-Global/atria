using Atria.Common.Models.Generic;
using Atria.Core.Data.Context;
using Atria.Core.Data.Entities.Outputs;
using Atria.Core.Data.Extensions;
using Atria.Core.Data.Models.Query;
using Atria.Core.Data.Repositories.Context.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Atria.Core.Data.Repositories.Context;

public class OutputRepository : Repository<Guid, Output>, IOutputRepository
{
    public OutputRepository(AtriaDbContext context)
        : base(context)
    {
    }

    public async Task<PagedList<Output>> GetOutputsAsync(
        QueryOptions<Output> queryOptions,
        CancellationToken cancellationToken)
    {
        var query = GetSet().AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryOptions.SearchQuery))
        {
            query = query.ApplyOutputSearch(queryOptions.SearchQuery);
        }

        var items = await query
            .Include(x => x.OutputTags)
            .ThenInclude(x => x.Tag)
            .ApplyQueryOptions(queryOptions)
            .ToListAsync(cancellationToken);

        var countQuery = GetSet().AsQueryable();
        if (!string.IsNullOrWhiteSpace(queryOptions.SearchQuery))
        {
            countQuery = countQuery.ApplyOutputSearch(queryOptions.SearchQuery);
        }

        var count = await countQuery.Filter(queryOptions).CountAsync(cancellationToken);

        return new PagedList<Output>
        {
            Items = items,
            TotalCount = count,
        };
    }
}
