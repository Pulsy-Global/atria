using Atria.Common.Models.Generic;
using Atria.Common.Web.Controllers;
using Atria.Common.Web.OData.Models;
using Atria.Core.Business.Facades;
using Atria.Core.Business.Models.Dto.Feed;
using Atria.Core.Business.Models.Dto.Output;
using Microsoft.AspNetCore.Mvc;

namespace Atria.Core.Api.Controllers;

[Route("feeds")]
public class FeedController(FeedFacade feedFacade)
    : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<FeedDto>> CreateFeedAsync(
        [FromBody] CreateFeedDto dto,
        CancellationToken ct)
    {
        var result = await feedFacade.CreateFeedAsync(dto, ct);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FeedDto>> UpdateFeedAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateFeedDto dto,
        CancellationToken ct)
    {
        var result = await feedFacade.UpdateFeedAsync(id, dto, ct);
        return Ok(result);
    }

    [HttpGet("{id}/deploys")]
    public async Task<ActionResult<List<DeployDto>>> GetFeedDeploysAsync([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await feedFacade.GetDeploysByFeedIdAsync(id, ct);
        return Ok(result);
    }

    [HttpGet("{id}/outputs")]
    public async Task<ActionResult<List<OutputDto>>> GetFeedOutputsAsync([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await feedFacade.GetOutputsByFeedIdAsync(id, ct);
        return Ok(result);
    }

    [HttpGet("{id}/results")]
    public async Task<ActionResult<List<ResultDto>>> GetFeedResultsAsync(
        [FromRoute] Guid id,
        [FromQuery] int limit = 1,
        CancellationToken ct = default)
    {
        var result = await feedFacade.GetResultsByFeedIdAsync(id, limit, ct);
        return Ok(result);
    }

    [HttpGet("{id}/results/stream")]
    public Task StreamFeedResultsAsync(
        [FromRoute] Guid id,
        [FromQuery] ulong? afterSeq = null,
        CancellationToken ct = default)
        => StreamSseAsync(feedFacade.StreamResultsByFeedIdAsync(id, afterSeq, ct), ct);

    [HttpGet("statuses")]
    public Task GetStreamFeedStatusesAsync(
        [FromQuery] string chainId,
        [FromQuery] IEnumerable<Guid> feedIds,
        CancellationToken ct = default)
        => StreamSseAsync(feedFacade.StreamStatusesByChainAsync(chainId, feedIds, ct), ct);

    [HttpGet("{id}")]
    public async Task<ActionResult<FeedDto>> GetFeedAsync([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await feedFacade.GetFeedAsync(id, ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedList<FeedDto>>> GetFeedsAsync(
        [FromQuery] ODataQueryParams<FeedDto> queryParams,
        CancellationToken ct)
    {
        var result = await feedFacade.GetFeedsAsync(queryParams, ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteFeedAsync([FromRoute] Guid id, CancellationToken ct)
    {
        await feedFacade.DeleteFeedAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id}/start")]
    public async Task<ActionResult> StartFeedAsync(
        [FromRoute] Guid id,
        [FromQuery] bool resetCursor = false,
        CancellationToken ct = default)
    {
        await feedFacade.StartFeedAsync(id, resetCursor, ct);
        return Ok();
    }

    [HttpPost("{id}/pause")]
    public async Task<ActionResult> PauseFeedAsync([FromRoute] Guid id, CancellationToken ct)
    {
        await feedFacade.PauseFeedAsync(id, ct);
        return Ok();
    }

    [HttpPost("test")]
    public async Task<ActionResult<TestResultDto>> TestFeedAsync(
        [FromBody] TestRequestDto dto,
        CancellationToken ct)
    {
        var result = await feedFacade.TestFeedAsync(dto, ct);
        return Ok(result);
    }
}
