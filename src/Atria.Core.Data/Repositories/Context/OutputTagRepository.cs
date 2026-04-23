using Atria.Core.Data.Context;
using Atria.Core.Data.Entities.Tags;
using Atria.Core.Data.Repositories.Context.Interfaces;

namespace Atria.Core.Data.Repositories.Context;

public class OutputTagRepository : Repository<Guid, OutputTag>, IOutputTagRepository
{
    public OutputTagRepository(AtriaDbContext context)
        : base(context)
    {
    }
}
