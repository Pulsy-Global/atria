using Atria.Common.Models.Generic;
using Atria.Core.Data.Entities.Outputs;
using Atria.Core.Data.Models.Query;

namespace Atria.Core.Data.Repositories.Context.Interfaces;

public interface IOutputRepository : IRepository<Guid, Output>
{
    Task<PagedList<Output>> GetOutputsAsync(
        QueryOptions<Output> queryOptions,
        CancellationToken ct);
}
