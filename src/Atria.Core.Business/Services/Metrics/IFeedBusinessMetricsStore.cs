using Atria.Core.Business.Models.Metrics;

namespace Atria.Core.Business.Services.Metrics;

public interface IFeedBusinessMetricsStore
{
    Task<MetricsStoreResult> QueryAsync(MetricsStoreQuery query, CancellationToken ct);
}
