using Atria.Common.Web.Controllers;
using Atria.Core.Business.Facades;
using Atria.Core.Business.Models.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace Atria.Core.Api.Controllers;

[Route("feeds")]
public sealed class FeedMetricsController(FeedMetricsFacade facade)
    : ApiControllerBase
{
    [HttpGet("{feedId}/metrics")]
    public async Task<ActionResult<FeedMetricsDto>> GetAsync(
        [FromRoute] Guid feedId,
        [FromQuery] MetricsRange range = MetricsRange.Last24Hours,
        CancellationToken ct = default)
    {
        return Ok(await facade.GetAsync(feedId, range, ct));
    }
}
