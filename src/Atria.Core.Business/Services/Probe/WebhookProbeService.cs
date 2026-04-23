using Atria.Common.Net;
using Atria.Common.Observability;
using Atria.Core.Business.Models.Dto.Output.Config;
using Atria.Core.Data.Entities.Enums;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;

namespace Atria.Core.Business.Services.Probe;

public class WebhookProbeService : IWebhookProbeService
{
    private const int ProbeTimeoutSeconds = 5;
    private const string ProbeHeaderName = "X-Atria-Probe";
    private const string ProbeHeaderValue = "1";

    private static readonly object _probePayload = new { probe = true };

    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookProbeService> _logger;

    public WebhookProbeService(HttpClient httpClient, ILogger<WebhookProbeService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task ProbeAsync(WebhookDto config, CancellationToken ct)
    {
        using var request = BuildRequest(config);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(ProbeTimeoutSeconds));

        using var response = await SendAsync(request, cts.Token, ct);

        EnsureSuccessfulProbeResponse(response);
    }

    private static HttpRequestMessage BuildRequest(WebhookDto config)
    {
        var method = config.Method == WebhookHttpMethod.Put ? HttpMethod.Put : HttpMethod.Post;

        var request = new HttpRequestMessage(method, config.Url)
        {
            Content = JsonContent.Create(_probePayload),
        };

        request.Headers.TryAddWithoutValidation(ProbeHeaderName, ProbeHeaderValue);

        if (config.Headers is { Count: > 0 })
        {
            foreach (var (key, value) in config.Headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        return request;
    }

    private static void EnsureSuccessfulProbeResponse(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;

        if (status is >= 300 and < 400)
        {
            throw new WebhookProbeException(
                WebhookProbeFailureReason.RedirectAttempted,
                "Webhook URL responded with a redirect. Redirects are not allowed.");
        }

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            throw new WebhookProbeException(
                WebhookProbeFailureReason.NotFound,
                "Webhook endpoint returned 404/410 — path does not exist.");
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken linkedToken,
        CancellationToken callerToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, linkedToken);
        }
        catch (HttpRequestException ex) when (ex.GetBaseException() is SsrfBlockedException ssrf)
        {
            _logger.LogSecurityEvent(
                SecurityEvents.SsrfBlocked,
                ex,
                "Webhook probe blocked by SSRF guard: {Host}",
                ssrf.Host);

            throw new WebhookProbeException(
                WebhookProbeFailureReason.Ssrf,
                "Webhook URL resolves to a forbidden address (loopback, private, or link-local).");
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
        {
            throw new WebhookProbeException(
                WebhookProbeFailureReason.Timeout,
                $"Webhook endpoint did not respond within {ProbeTimeoutSeconds} seconds.");
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            throw new WebhookProbeException(
                WebhookProbeFailureReason.Dns,
                "Webhook URL could not be resolved or connected to.");
        }
        catch (HttpRequestException ex)
        {
            throw new WebhookProbeException(
                WebhookProbeFailureReason.Unreachable,
                $"Webhook endpoint is unreachable: {ex.Message}");
        }
    }
}
