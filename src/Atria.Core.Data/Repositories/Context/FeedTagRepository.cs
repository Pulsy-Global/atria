using Atria.Core.Data.Context;
using Atria.Core.Data.Entities.Tags;
using Atria.Core.Data.Repositories.Context.Interfaces;

namespace Atria.Core.Data.Repositories.Context;

public class FeedTagRepository : Repository<Guid, FeedTag>, IFeedTagRepository
{
    public FeedTagRepository(AtriaDbContext context)
        : base(context)
    {
    }
}
