using Atria.Common.Web.Models.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

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

        var telemetryOptions = TelemetryConfiguration.GetRequiredOptions(builder);
        var otlpEndpoint = TelemetryConfiguration.GetRequiredOtlpEndpoint(telemetryOptions);

        builder.Host.UseSerilog((_, cfg) =>
            cfg
                .ReadFrom.Configuration(configuration)
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = otlpEndpoint.AbsoluteUri;
                    options.Protocol = OtlpProtocol.Grpc;
                    options.ResourceAttributes = CreateLogResourceAttributes(builder, telemetryOptions);
                    options.RestrictedToMinimumLevel = LogEventLevel.Information;
                    options.OnBeginSuppressInstrumentation = SuppressInstrumentationScope.Begin;
                }));

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

    private static Dictionary<string, object> CreateLogResourceAttributes(
        WebApplicationBuilder builder,
        TelemetryOptions options)
    {
        var attributes = TelemetryConfiguration.CreateResourceAttributes(builder, options);

        attributes["service.name"] = options.ServiceName;
        attributes["service.instance.id"] = TelemetryConfiguration.GetServiceInstanceId();

        return attributes;
    }
}
