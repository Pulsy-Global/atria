using Atria.Business.Services.Namespaces.Interfaces;
using Atria.Core.Business.Models.Metrics;
using Atria.Core.Business.Services.Metrics;

namespace Atria.Core.Business.Facades;

public sealed class FeedMetricsFacade
{
    private readonly FeedFacade _feedFacade;
    private readonly IResourceNamespaceResolver _resourceNamespaceResolver;
    private readonly IFeedMetricsQueryService _queryService;

    public FeedMetricsFacade(
        FeedFacade feedFacade,
        IResourceNamespaceResolver resourceNamespaceResolver,
        IFeedMetricsQueryService queryService)
    {
        _feedFacade = feedFacade;
        _resourceNamespaceResolver = resourceNamespaceResolver;
        _queryService = queryService;
    }

    public async Task<FeedMetricsDto> GetAsync(Guid feedId, MetricsRange range, CancellationToken ct)
    {
        await _feedFacade.GetFeedAsync(feedId, ct);
        var resourceNamespace = await _resourceNamespaceResolver.ResolveForFeedAsync(feedId, ct);

        return await _queryService.GetAsync(new FeedMetricsScope(resourceNamespace, feedId), range, ct);
    }
}
