namespace Atria.Core.Business.Models.Options;

public sealed class FeedMetricsOptions
{
    public const string SectionName = "BusinessMetrics";

    public string QueryBaseUrl { get; set; } = "http://victoria-metrics:8428/prometheus";

    public int QueryTimeoutSeconds { get; set; } = 5;

    public int CacheSeconds { get; set; } = 30;
}
