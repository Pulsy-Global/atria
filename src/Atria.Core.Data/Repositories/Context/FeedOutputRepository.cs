using Atria.Core.Data.Context;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Repositories.Context.Interfaces;

namespace Atria.Core.Data.Repositories.Context;

public class FeedOutputRepository : Repository<Guid, FeedOutput>, IFeedOutputRepository
{
    public FeedOutputRepository(AtriaDbContext context)
        : base(context)
    {
    }
}
