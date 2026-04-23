using Atria.Common.Models.Generic;
using Atria.Core.Business.Models.Dto.Output;
using Atria.Core.Data.Models.Query;

namespace Atria.Core.Business.Managers.Interfaces;

public interface IOutputManager : IBaseManager
{
    Task<OutputDto> CreateOutputAsync(CreateOutputDto dto, CancellationToken ct);

    Task<OutputDto> UpdateOutputAsync(Guid id, UpdateOutputDto dto, CancellationToken ct);

    Task<OutputDto> GetOutputAsync(Guid id, CancellationToken ct);

    Task<PagedList<OutputDto>> GetOutputsAsync(QueryOptions<OutputDto> queryOptions, CancellationToken ct);

    Task<List<OutputDto>> GetOutputsByFeedIdAsync(Guid feedId, CancellationToken ct);

    Task DeleteOutputAsync(Guid id, CancellationToken ct);
}
