namespace Atria.Common.Web.Models.Options;

public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public string ServiceName { get; set; } = string.Empty;

    public TelemetryComponent? Component { get; set; }

    public string Environment { get; set; } = string.Empty;

    public string OtlpEndpoint { get; set; } = "http://otel-collector:4317";

    public int MetricExportIntervalSeconds { get; set; } = 30;
}
