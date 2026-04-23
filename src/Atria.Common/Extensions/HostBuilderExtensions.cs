using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Atria.Common.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder ConfigureAtria(
        this IHostBuilder hostBuilder,
        string? settingsFolderName = null)
    {
        return hostBuilder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddSettingsFolder(settingsFolderName)
                .AddEnvironmentVariables();
        });
    }
}
