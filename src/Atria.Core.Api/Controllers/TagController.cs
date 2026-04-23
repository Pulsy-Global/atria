using Atria.Common.Web.Controllers;
using Atria.Core.Business.Facades;
using Atria.Core.Business.Models.Dto.Tag;
using Atria.Core.Data.Entities.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Atria.Core.Api.Controllers;

[Route("tags")]
public class TagController : ApiControllerBase
{
    private readonly TagFacade _tagFacade;

    public TagController(TagFacade tagFacade)
    {
        _tagFacade = tagFacade;
    }

    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTagAsync(
        [FromBody] CreateTagDto dto,
        CancellationToken ct)
    {
        var result = await _tagFacade.CreateTagAsync(dto, ct);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TagDto>> UpdateTagAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateTagDto dto,
        CancellationToken ct)
    {
        var result = await _tagFacade.UpdateTagAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTagAsync([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _tagFacade.GetTagAsync(id, ct);
        return Ok(result);
    }

    [HttpGet("feed")]
    public async Task<ActionResult<List<TagDto>>> GetFeedTagsAsync(CancellationToken ct)
    {
        var result = await _tagFacade.GetTagsByTypeAsync(TagType.Feed, ct);
        return Ok(result);
    }

    [HttpGet("output")]
    public async Task<ActionResult<List<TagDto>>> GetOutputTagsAsync(CancellationToken ct)
    {
        var result = await _tagFacade.GetTagsByTypeAsync(TagType.Output, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTagAsync([FromRoute] Guid id, CancellationToken ct)
    {
        await _tagFacade.DeleteTagAsync(id, ct);
        return NoContent();
    }
}
