using Atria.Common.Models.Generic;
using Atria.Common.Web.OData.Models;
using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Dto.Output;
using Atria.Core.Data.Extensions;

namespace Atria.Core.Business.Facades;

public class OutputFacade
{
    private readonly IOutputManager _outputManager;

    public OutputFacade(IOutputManager outputManager)
    {
        _outputManager = outputManager;
    }

    public async Task<OutputDto> CreateOutputAsync(CreateOutputDto dto, CancellationToken ct) =>
        await _outputManager.CreateOutputAsync(dto, ct);

    public async Task<OutputDto> UpdateOutputAsync(Guid id, UpdateOutputDto dto, CancellationToken ct) =>
        await _outputManager.UpdateOutputAsync(id, dto, ct);

    public async Task<OutputDto> GetOutputAsync(Guid id, CancellationToken ct) =>
        await _outputManager.GetOutputAsync(id, ct);

    public async Task<PagedList<OutputDto>> GetOutputsAsync(ODataQueryParams<OutputDto> queryParams, CancellationToken ct) =>
        await _outputManager.GetOutputsAsync(queryParams.ToQueryOptions(), ct);

    public async Task DeleteOutputAsync(Guid id, CancellationToken ct) =>
        await _outputManager.DeleteOutputAsync(id, ct);
}
