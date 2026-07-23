using System.Diagnostics.Metrics;

namespace Atria.Common.Observability;

public static class AtriaMeters
{
    public const string ObservabilityMeterName = "Atria.Observability";
    public const string BusinessMetricsMeterName = "Atria.BusinessMetrics";

    public static readonly Meter Observability = new(ObservabilityMeterName);
    public static readonly Meter BusinessMetrics = new(BusinessMetricsMeterName);
}
