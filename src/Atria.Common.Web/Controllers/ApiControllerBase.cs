using Atria.Common.Web.Configuration;
using Atria.Common.Web.Swagger.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Atria.Common.Web.Controllers;

[ApiController]
[Route("v{version:apiVersion}")]
[ApiExplorerSettings(GroupName = SwaggerArea.MainApi)]
[ApiConventionType(typeof(Conventions))]
[ApiVersion("1.0")]
[ResponseCache(
    Duration = 0,
    Location = ResponseCacheLocation.None,
    NoStore = true)]
public class ApiControllerBase : ControllerBase
{
    private static readonly JsonSerializerOptions _sseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    protected async Task StreamSseAsync<T>(
        IAsyncEnumerable<T> stream,
        CancellationToken ct = default)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await foreach (var item in stream.WithCancellation(ct))
            {
                var json = JsonSerializer.Serialize(item, _sseJsonOptions);
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected — expected for SSE connections
        }
    }
}
