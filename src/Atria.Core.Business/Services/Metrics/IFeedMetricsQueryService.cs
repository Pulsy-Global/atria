using Atria.Core.Business.Models.Metrics;

namespace Atria.Core.Business.Services.Metrics;

public interface IFeedMetricsQueryService
{
    Task<FeedMetricsDto> GetAsync(
        FeedMetricsScope scope,
        MetricsRange range,
        CancellationToken ct);
}
