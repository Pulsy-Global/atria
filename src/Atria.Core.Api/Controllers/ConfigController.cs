using Atria.Common.Web.Controllers;
using Atria.Core.Business.Facades;
using Atria.Core.Business.Models.Dto.Network;
using Microsoft.AspNetCore.Mvc;

namespace Atria.Core.Api.Controllers;

[Route("config")]
public class ConfigController : ApiControllerBase
{
    private readonly ConfigFacade _configFacade;

    public ConfigController(ConfigFacade configFacade)
    {
        _configFacade = configFacade;
    }

    [HttpGet("networks")]
    public ActionResult<NetworksDto> GetNetworks()
    {
        var result = _configFacade.GetNetworks();
        return Ok(result);
    }

    [HttpGet("networks/{networkId}/block")]
    public async Task<ActionResult<LatestBlockDto>> GetLatestBlock([FromRoute] string networkId)
    {
        var result = await _configFacade.GetLatestBlockAsync(networkId);
        return Ok(result);
    }
}
