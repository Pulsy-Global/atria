using Atria.Common.Web.Extensions;
using Atria.Common.Web.Models.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Atria.Core.Spa.Controllers;

[Route("configuration")]
public class ConfigurationController : Controller
{
    private readonly ConfigurationOptions _configOptions;

    public ConfigurationController(IOptions<ConfigurationOptions> options)
    {
        _configOptions = options.Value;
    }

    [HttpGet]
    [Route("spa")]
    public IActionResult GetSpaConfiguration()
    {
        return _configOptions.GenerateSpaConfiguration();
    }
}
