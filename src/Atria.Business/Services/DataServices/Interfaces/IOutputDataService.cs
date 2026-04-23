using Atria.Common.Models.Generic;
using Atria.Core.Data.Entities.Outputs;
using Atria.Core.Data.Entities.Outputs.Config;
using Atria.Core.Data.Models.Query;
using System.Linq.Expressions;

namespace Atria.Business.Services.DataServices.Interfaces;

public interface IOutputDataService
{
    Task<Output> CreateOutputAsync(Output entity, CancellationToken ct, List<Guid>? tagIds = null);

    Task<Output> UpdateOutputAsync(Output entity, CancellationToken ct, List<Guid>? tagIds = null);

    Task<Output> GetOutputByIdAsync(Guid id, CancellationToken ct, params Expression<Func<Output, object>>[] includes);

    Task<List<Output>> GetOutputsAsync(Expression<Func<Output, bool>> predicate, CancellationToken ct, params Expression<Func<Output, object>>[] includes);

    Task<PagedList<Output>> GetOutputsAsync(QueryOptions<Output> queryOptions, CancellationToken ct);

    Task DeleteOutputAsync(Guid id, CancellationToken ct);

    TConfig? GetTypedConfig<TConfig>(Output output)
        where TConfig : OutputConfigBase;
}
