using Atria.Common.Web.Models.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Atria.Common.Web.Configuration;

internal static class TelemetryConfiguration
{
    public static TelemetryOptions GetRequiredOptions(WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(TelemetryOptions.SectionName);
        var options = section.Get<TelemetryOptions>() ?? new TelemetryOptions();

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            throw new InvalidOperationException(
                $"{TelemetryOptions.SectionName}:{nameof(TelemetryOptions.ServiceName)} is required.");
        }

        if (options.Component == null)
        {
            throw new InvalidOperationException(
                $"{TelemetryOptions.SectionName}:{nameof(TelemetryOptions.Component)} is required.");
        }

        return options;
    }

    public static Uri GetRequiredOtlpEndpoint(TelemetryOptions options)
    {
        if (!Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                $"{TelemetryOptions.SectionName}:{nameof(TelemetryOptions.OtlpEndpoint)} must be an absolute URI.");
        }

        return endpoint;
    }

    public static Dictionary<string, object> CreateResourceAttributes(
        WebApplicationBuilder builder,
        TelemetryOptions options)
    {
        var component = options.Component!.Value.ToString().ToLowerInvariant();

        return new Dictionary<string, object>
        {
            ["atria.component"] = component,
            ["deployment.environment.name"] = builder.Environment.EnvironmentName,
            ["service.namespace"] = "atria",
        };
    }

    public static string GetServiceInstanceId()
    {
        var instanceId = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;

        return instanceId;
    }
}
