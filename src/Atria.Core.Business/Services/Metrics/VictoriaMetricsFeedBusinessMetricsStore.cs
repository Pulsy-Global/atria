using Atria.Core.Business.Models.Metrics;
using Atria.Core.Business.Models.Metrics.VictoriaMetrics;
using Atria.Core.Business.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;

namespace Atria.Core.Business.Services.Metrics;

public sealed class VictoriaMetricsFeedBusinessMetricsStore : IFeedBusinessMetricsStore
{
    private readonly HttpClient _httpClient;
    private readonly FeedMetricsOptions _options;
    private readonly ILogger<VictoriaMetricsFeedBusinessMetricsStore> _logger;

    public VictoriaMetricsFeedBusinessMetricsStore(
        HttpClient httpClient,
        IOptions<FeedMetricsOptions> options,
        ILogger<VictoriaMetricsFeedBusinessMetricsStore> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MetricsStoreResult> QueryAsync(MetricsStoreQuery query, CancellationToken ct)
    {
        if (!Uri.TryCreate(_options.QueryBaseUrl, UriKind.Absolute, out var baseUri))
        {
            return MetricsStoreResult.Unavailable;
        }

        var requestUri = BuildRequestUri(baseUri, query);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.QueryTimeoutSeconds)));

        try
        {
            using var response = await _httpClient.GetAsync(
                requestUri,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Business metrics query failed with HTTP status {StatusCode}",
                    (int)response.StatusCode);

                return MetricsStoreResult.Unavailable;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            var payload = await JsonSerializer.DeserializeAsync<VictoriaMetricsQueryResponse>(
                stream,
                cancellationToken: timeoutCts.Token);

            return Parse(payload);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Business metrics query is unavailable");

            return MetricsStoreResult.Unavailable;
        }
    }

    private static Uri BuildRequestUri(Uri baseUri, MetricsStoreQuery query)
    {
        var endpoint = new Uri($"{baseUri.ToString().TrimEnd('/')}/api/v1/query_range");
        var queryString = string.Join(
            "&",
            $"query={Uri.EscapeDataString(query.Expression)}",
            $"start={query.Start.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}",
            $"end={query.End.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}",
            $"step={((int)query.Step.TotalSeconds).ToString(CultureInfo.InvariantCulture)}");

        return new UriBuilder(endpoint) { Query = queryString }.Uri;
    }

    private static MetricsStoreResult Parse(VictoriaMetricsQueryResponse? payload)
    {
        if (payload?.Status != "success"
            || payload.Data?.ResultType != "matrix"
            || payload.Data.Result == null)
        {
            return MetricsStoreResult.Unavailable;
        }

        var series = new List<MetricsStoreSeries>();
        foreach (var item in payload.Data.Result)
        {
            var parsed = ParseSeries(item);
            if (parsed == null)
            {
                return MetricsStoreResult.Unavailable;
            }

            series.Add(parsed);
        }

        return new MetricsStoreResult(true, series);
    }

    private static MetricsStoreSeries? ParseSeries(VictoriaMetricsSeries item)
    {
        if (item.Metric == null || item.Values == null)
        {
            return null;
        }

        var labels = item.Metric.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        var points = item.Values
            .Select(value => new FeedMetricPointDto(
                DateTimeOffset.FromUnixTimeMilliseconds((long)(value.Timestamp * 1000)),
                value.Value))
            .ToArray();

        return new MetricsStoreSeries(labels, points);
    }
}
