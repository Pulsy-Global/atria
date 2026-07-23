using Atria.Core.Business.Models.Metrics;
using Atria.Core.Business.Models.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Atria.Core.Business.Services.Metrics;

public sealed class FeedMetricsQueryService : IFeedMetricsQueryService
{
    private readonly IFeedBusinessMetricsStore _store;
    private readonly IMemoryCache _cache;
    private readonly FeedMetricsOptions _options;
    private readonly TimeProvider _timeProvider;

    public FeedMetricsQueryService(
        IFeedBusinessMetricsStore store,
        IMemoryCache cache,
        IOptions<FeedMetricsOptions> options,
        TimeProvider timeProvider)
    {
        _store = store;
        _cache = cache;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<FeedMetricsDto> GetAsync(
        FeedMetricsScope scope,
        MetricsRange range,
        CancellationToken ct)
    {
        var definition = MetricsRangeCatalog.Get(range);
        var now = _timeProvider.GetUtcNow();
        var end = AlignDown(now, definition.Resolution);
        var start = end - definition.Duration;
        var cacheKey = CreateCacheKey(scope, definition, end);

        if (_cache.TryGetValue(cacheKey, out FeedMetricsDto? cached) && cached != null)
        {
            return cached;
        }

        var results = await QueryStoreAsync(scope, definition, start, end, ct);
        var response = FeedMetricsResponseFactory.Create(
            scope.FeedId,
            definition,
            now,
            start,
            end,
            results);
        _cache.Set(
            cacheKey,
            response,
            GetCacheLifetime(response.Status, definition, now, end));

        return response;
    }

    private static DateTimeOffset AlignDown(DateTimeOffset value, TimeSpan resolution)
    {
        var ticks = value.UtcTicks - (value.UtcTicks % resolution.Ticks);

        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    private static string CreateCacheKey(
        FeedMetricsScope scope,
        MetricsRangeDefinition definition,
        DateTimeOffset end)
    {
        return $"feed-metrics:{scope.ResourceNamespace}:{scope.FeedId}:{definition.Value}:{end.ToUnixTimeSeconds()}";
    }

    private async Task<IReadOnlyDictionary<string, MetricsStoreResult>> QueryStoreAsync(
        FeedMetricsScope scope,
        MetricsRangeDefinition definition,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken ct)
    {
        var queryGroups = FeedMetricsQueryFactory.Create(scope, start, end, definition.Resolution);
        var tasks = queryGroups.Select(async group =>
            (group.Key, Result: await _store.QueryAsync(group.Query, ct)));
        var results = await Task.WhenAll(tasks);

        return results.ToDictionary(item => item.Key, item => item.Result);
    }

    private TimeSpan GetCacheLifetime(
        string status,
        MetricsRangeDefinition definition,
        DateTimeOffset now,
        DateTimeOffset end)
    {
        var configuredLifetime = TimeSpan.FromSeconds(Math.Max(1, _options.CacheSeconds));
        if (status == MetricsAvailability.Unavailable)
        {
            return TimeSpan.FromSeconds(Math.Min(5, configuredLifetime.TotalSeconds));
        }

        var untilNextBoundary = end + definition.Resolution - now;
        if (untilNextBoundary <= TimeSpan.Zero)
        {
            return configuredLifetime;
        }

        return untilNextBoundary < configuredLifetime
            ? untilNextBoundary
            : configuredLifetime;
    }
}
