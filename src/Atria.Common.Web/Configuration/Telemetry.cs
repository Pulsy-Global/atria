using Atria.Common.Observability;
using Atria.Common.Web.Models.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Atria.Common.Web.Configuration;

public static class Telemetry
{
    public static WebApplicationBuilder AddAtriaTelemetry(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(TelemetryOptions.SectionName);
        builder.Services.Configure<TelemetryOptions>(section);

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

        if (!Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                $"{TelemetryOptions.SectionName}:{nameof(TelemetryOptions.OtlpEndpoint)} must be an absolute URI.");
        }

        var exportInterval = TimeSpan.FromSeconds(Math.Max(1, options.MetricExportIntervalSeconds));
        var instanceId = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;
        var component = options.Component.Value.ToString().ToLowerInvariant();

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(options.ServiceName, serviceInstanceId: instanceId)
                .AddAttributes(
                [
                    new KeyValuePair<string, object>("atria.component", component),
                    new KeyValuePair<string, object>("deployment.environment.name", builder.Environment.EnvironmentName),
                ]))
            .WithMetrics(metrics => metrics
                .AddMeter(AtriaMeters.ObservabilityMeterName, AtriaMeters.BusinessMetricsMeterName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddView(
                    "atria.runtime.block.processing.duration",
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30],
                    })
                .AddView(
                    "atria.delivery.target.duration",
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = [0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30],
                    })
                .AddOtlpExporter((exporter, metricReader) =>
                {
                    exporter.Endpoint = endpoint;
                    exporter.Protocol = OtlpExportProtocol.Grpc;
                    metricReader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds =
                        (int)exportInterval.TotalMilliseconds;
                }));

        return builder;
    }
}
