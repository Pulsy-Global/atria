using Atria.Core.Business.Models.Metrics;

namespace Atria.Core.Business.Services.Metrics;

public static class MetricsRangeCatalog
{
    public static MetricsRangeDefinition Get(MetricsRange range)
    {
        return range switch
        {
            MetricsRange.LastHour => new(range, "lastHour", TimeSpan.FromHours(1), TimeSpan.FromMinutes(1)),
            MetricsRange.Last24Hours => new(range, "last24Hours", TimeSpan.FromHours(24), TimeSpan.FromMinutes(15)),
            MetricsRange.Last7Days => new(range, "last7Days", TimeSpan.FromDays(7), TimeSpan.FromHours(1)),
            MetricsRange.Last30Days => new(range, "last30Days", TimeSpan.FromDays(30), TimeSpan.FromHours(6)),
            _ => throw new ArgumentOutOfRangeException(nameof(range), range, "Unsupported metrics range."),
        };
    }
}
