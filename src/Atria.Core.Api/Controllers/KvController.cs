using Atria.Common.Web.Controllers;
using Atria.Core.Business.Facades;
using Atria.Core.Business.Models.Dto.Kv;
using Microsoft.AspNetCore.Mvc;

namespace Atria.Core.Api.Controllers;

[Route("kv")]
public class KvController(KvFacade kvFacade)
    : ApiControllerBase
{
    [HttpPost("{bucket}")]
    public async Task<ActionResult> AddBucketAsync(
        [FromRoute] string bucket,
        [FromBody] BucketItemDto dto)
    {
        await kvFacade.AddBucketAsync(bucket, dto);
        return Ok();
    }

    [HttpGet("{bucket}")]
    public async Task<ActionResult<BucketValueDto>> GetBucketAsync(
        [FromRoute] string bucket,
        [FromQuery] string key)
    {
        var result = await kvFacade.GetBucketValueAsync(bucket, key);
        return Ok(result);
    }

    [HttpDelete("{bucket}")]
    public async Task<ActionResult> DeleteBucketAsync(
        [FromRoute] string bucket,
        [FromQuery] string key)
    {
        await kvFacade.DeleteBucketAsync(bucket, key);
        return Ok();
    }

    [HttpGet("{bucket}/values")]
    public async Task<ActionResult<BucketValuesDto>> GetBucketValuesAsync(
        [FromRoute] string bucket,
        [FromQuery] int limit,
        [FromQuery] string? cursor = null)
        => await kvFacade.GetBucketValuesAsync(bucket, limit, cursor);

    [HttpPost("{bucket}/batch")]
    public async Task<ActionResult> AddBucketBatchAsync(
        [FromRoute] string bucket,
        [FromBody] AddBucketBatchDto dto)
    {
        await kvFacade.AddBucketBatchAsync(bucket, dto);
        return Ok();
    }

    [HttpDelete("{bucket}/batch")]
    public async Task<ActionResult> DeleteBucketBatchAsync(
        [FromRoute] string bucket,
        [FromBody] BucketBatchKeysDto dto)
    {
        await kvFacade.DeleteBucketBatchAsync(bucket, dto);
        return Ok();
    }

    [HttpGet("{bucket}/batch")]
    public async Task<ActionResult<BucketBatchItemsDto>> GetBucketBatchAsync(
        [FromRoute] string bucket,
        [FromQuery] string keys)
    {
        var result = await kvFacade.GetBucketBatchAsync(bucket, new BucketBatchKeysDto(keys));
        return Ok(result);
    }
}
