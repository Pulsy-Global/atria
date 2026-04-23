using Atria.Common.Models.Generic;
using Atria.Common.Web.Controllers;
using Atria.Common.Web.OData.Models;
using Atria.Core.Business.Facades;
using Atria.Core.Business.Models.Dto.Output;
using Microsoft.AspNetCore.Mvc;

namespace Atria.Core.Api.Controllers;

[Route("outputs")]
public class OutputController : ApiControllerBase
{
    private readonly OutputFacade _outputFacade;

    public OutputController(OutputFacade outputFacade)
    {
        _outputFacade = outputFacade;
    }

    [HttpPost]
    public async Task<ActionResult<OutputDto>> CreateOutputAsync(
        [FromBody] CreateOutputDto dto,
        CancellationToken ct)
    {
        var result = await _outputFacade.CreateOutputAsync(dto, ct);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<OutputDto>> UpdateOutputAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateOutputDto dto,
        CancellationToken ct)
    {
        var result = await _outputFacade.UpdateOutputAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OutputDto>> GetOutputAsync([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _outputFacade.GetOutputAsync(id, ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedList<OutputDto>>> GetOutputsAsync(
        [FromQuery] ODataQueryParams<OutputDto> queryParams,
        CancellationToken ct)
    {
        var result = await _outputFacade.GetOutputsAsync(queryParams, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOutputAsync([FromRoute] Guid id, CancellationToken ct)
    {
        await _outputFacade.DeleteOutputAsync(id, ct);
        return NoContent();
    }
}
