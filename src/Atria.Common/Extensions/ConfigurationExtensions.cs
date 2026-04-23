using Microsoft.Extensions.Configuration;

namespace Atria.Common.Extensions;

public static class ConfigurationExtensions
{
    private const string DefaultSettingsFolder = "Settings";

    public static IConfigurationBuilder AddSettingsFolder(
        this IConfigurationBuilder configBuilder,
        string? settingsFolderName = null)
    {
        var folderName = settingsFolderName ?? DefaultSettingsFolder;
        var settingsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);

        if (!Directory.Exists(settingsFolderPath))
        {
            throw new DirectoryNotFoundException($"Settings folder not found: {settingsFolderPath}");
        }

        var configFiles = Directory.GetFiles(settingsFolderPath, "*.json");

        foreach (var configFilePath in configFiles)
        {
            configBuilder.AddJsonFile(configFilePath, optional: false, reloadOnChange: true);
        }

        return configBuilder;
    }
}
