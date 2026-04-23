using Atria.Feed.Runtime.Engine.Exceptions;
using Atria.Feed.Runtime.Engine.Functions.Options;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Services;

public class FissionHttpService
{
    private const string ExecuteHeaderName = "X-Atria-System-Execute";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly HttpClient _httpClient;
    private readonly FissionClientOptions _options;
    private readonly ILogger<FissionHttpService> _logger;

    public FissionHttpService(HttpClient httpClient, FissionClientOptions options, ILogger<FissionHttpService> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<object?> InvokeFunctionAsync(string functionId, object? input, CancellationToken ct)
    {
        _logger.LogDebug("Invoking function '{FunctionId}' via HTTP", functionId);

        var requestUrl = $"/{functionId}";
        var jsonContent = JsonSerializer.Serialize(input, JsonOptions);

        using var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        var responseContent = "";

        try
        {
            response = await _httpClient.PostAsync(requestUrl, httpContent, ct);
            responseContent = await response.Content.ReadAsStringAsync(ct);

            _logger.LogDebug("Function '{FunctionId}' HTTP response: {StatusCode}", functionId, (int)response.StatusCode);
        }
        catch (Exception httpEx) when (httpEx is TimeoutException || httpEx is TaskCanceledException)
        {
            _logger.LogWarning(httpEx, "HTTP trigger call failed for function '{FunctionId}'", functionId);
            throw new InvalidOperationException(
                $"HTTP trigger not available for function '{functionId}'. The function may not have an HTTP trigger configured.", httpEx);
        }

        if (!response.IsSuccessStatusCode)
        {
            HandleUnsuccessfulResponse(functionId, response.StatusCode, responseContent);
        }

        return string.IsNullOrEmpty(responseContent)
            ? null
            : JsonSerializer.Deserialize<object>(responseContent, JsonOptions);
    }

    public async Task<bool> IsFunctionReadyViaHttpAsync(string functionName, CancellationToken ct)
    {
        try
        {
            var triggerUrl = $"/{functionName}";
            using var request = new HttpRequestMessage(HttpMethod.Post, triggerUrl);
            request.Content = new StringContent("{\"test\": \"ping\"}", Encoding.UTF8, "application/json");
            request.Headers.Add(ExecuteHeaderName, "ping");

            using var response = await _httpClient.SendAsync(request, ct);

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.OK => true,
                System.Net.HttpStatusCode.NotFound => false,
                System.Net.HttpStatusCode.InternalServerError => false,
                _ => false,
            };
        }
        catch
        {
            return false;
        }
    }

    private void HandleUnsuccessfulResponse(string functionId, System.Net.HttpStatusCode statusCode, string responseContent)
    {
        switch (statusCode)
        {
            case System.Net.HttpStatusCode.NotFound:
            case System.Net.HttpStatusCode.InternalServerError:
                throw new FeedEngineException(responseContent, functionId, isFunctionError: true);
            default:
                throw new InvalidOperationException(
                    $"Function '{functionId}' failed with status code {statusCode}. Response: {responseContent}");
        }
    }
}
