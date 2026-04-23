using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Atria.Common.Web.Configuration;

public static class Infrastructure
{
    public static WebApplicationBuilder ConfigureKestrel(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        var kestrelPort = configuration.GetValue<int>("KestrelPort");

        builder.WebHost.ConfigureKestrel((context, serverOptions) =>
        {
            serverOptions.ListenAnyIP(kestrelPort);
        });

        return builder;
    }

    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder, IConfiguration configuration)
    {
        Serilog.Debugging.SelfLog.Enable(Console.Error);

        builder.Host.UseSerilog((ctx, cfg) =>
            cfg.ReadFrom.Configuration(configuration));

        builder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog();
        });

        return builder;
    }

    public static WebApplicationBuilder ConfigureOptionFiles(this WebApplicationBuilder builder)
    {
        var settingsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");

        if (Directory.Exists(settingsFolderPath))
        {
            var configFiles = Directory.GetFiles(settingsFolderPath, "*.json");

            foreach (var configFilePath in configFiles)
            {
                builder.Configuration.AddJsonFile(configFilePath, optional: false, reloadOnChange: true);
            }
        }

        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }
}
