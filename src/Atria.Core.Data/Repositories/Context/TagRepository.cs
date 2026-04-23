using Atria.Core.Data.Context;
using Atria.Core.Data.Entities.Tags;
using Atria.Core.Data.Repositories.Context.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Atria.Core.Data.Repositories.Context;

public class TagRepository : Repository<Guid, Tag>, ITagRepository
{
    public TagRepository(AtriaDbContext context)
        : base(context)
    {
    }

    public async Task<List<Tag>> GetTagsByTypeAsync(string type, CancellationToken ct)
    {
        return await Context.Set<Tag>()
            .Where(t => t.Type == type)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }
}
