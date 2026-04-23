using Atria.Common.Net;
using Atria.Common.Observability;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Atria.Feed.Delivery.FeedPipeline.Handlers.Delivery;

public class WebhookDeliveryHandler : IDeliveryHandler
{
    private const string FeedIdHeaderName = "X-Atria-Feed-Id";
    private const string TestExecutionHeaderName = "X-Atria-Test-Execution";

    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookDeliveryHandler> _logger;

    public TargetType SupportedTargetType => TargetType.Webhook;

    public WebhookDeliveryHandler(HttpClient httpClient, ILogger<WebhookDeliveryHandler> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task DeliverAsync(
        string feedId,
        FeedDeliveryTarget target,
        object? data,
        bool isTestExecution,
        CancellationToken ct = default)
    {
        if (target is not FeedWebhookTarget webhook)
        {
            throw new ArgumentException($"Expected WebhookTarget, got {target.GetType().Name}");
        }

        if (data is null)
        {
            return;
        }

        using var request = BuildRequest(feedId, webhook, data, isTestExecution);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(webhook.TimeoutSeconds));

        try
        {
            using var response = await _httpClient.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex) when (ex.GetBaseException() is SsrfBlockedException ssrf)
        {
            _logger.LogSecurityEvent(
                SecurityEvents.SsrfBlocked,
                ex,
                "Webhook delivery blocked by SSRF guard. Feed: {FeedId}, Host: {Host}",
                feedId,
                ssrf.Host);

            throw;
        }
    }

    private static HttpRequestMessage BuildRequest(
        string feedId,
        FeedWebhookTarget webhook,
        object payload,
        bool isTestExecution)
    {
        var request = new HttpRequestMessage(new HttpMethod(webhook.Method), webhook.Url)
        {
            Content = JsonContent.Create(payload),
        };

        request.Headers.Add(FeedIdHeaderName, feedId);

        if (isTestExecution)
        {
            request.Headers.Add(TestExecutionHeaderName, "true");
        }

        if (webhook.Headers is { Count: > 0 })
        {
            foreach (var (key, value) in webhook.Headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        return request;
    }
}
