using Atria.Common.Web.Models.Options;

namespace Atria.Common.Web.Models.Dto;

public class ConfigurationDto
{
    public ConfigurationDto(ConfigurationOptions spaConfiguration)
    {
        ApiServer = spaConfiguration.ApiServer;
    }

    public string ApiServer { get; set; }

    public string? Version { get; set; }

    public string? InformationalVersion { get; set; }

    public bool FunctionsEnabled { get; set; }
}
