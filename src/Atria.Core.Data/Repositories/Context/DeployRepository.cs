using Atria.Core.Data.Context;
using Atria.Core.Data.Entities.Deploys;
using Atria.Core.Data.Repositories.Context.Interfaces;

namespace Atria.Core.Data.Repositories.Context;

public class DeployRepository : Repository<Guid, Deploy>, IDeployRepository
{
    public DeployRepository(AtriaDbContext context)
        : base(context)
    {
    }
}
