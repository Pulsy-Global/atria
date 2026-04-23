using Atria.Common.Exceptions;
using Atria.Common.Web.Models.Errors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Atria.Common.Web.Middlewares;

public class ErrorWrappingMiddleware
{
    private static readonly ActionDescriptor _emptyActionDescriptor = new();
    private static readonly RouteData _emptyRouteData = new();
    private static readonly HashSet<string> _corsHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        HeaderNames.AccessControlAllowCredentials,
        HeaderNames.AccessControlAllowHeaders,
        HeaderNames.AccessControlAllowMethods,
        HeaderNames.AccessControlAllowOrigin,
        HeaderNames.AccessControlExposeHeaders,
        HeaderNames.AccessControlMaxAge,
    };

    private readonly RequestDelegate _next;
    private readonly IActionResultExecutor<ObjectResult> _executor;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ErrorWrappingMiddleware> _logger;

    public ErrorWrappingMiddleware(
        RequestDelegate next,
        IActionResultExecutor<ObjectResult> executor,
        IWebHostEnvironment environment,
        ILogger<ErrorWrappingMiddleware> logger)
    {
        _next = next;
        _executor = executor;
        _environment = environment;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next.Invoke(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected — not an error
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            try
            {
                _logger.LogWarning($"{ex}. TraceId: {context.TraceIdentifier}");

                var problemDetails = ex switch
                {
                    CursorBehindTailException cursorEx => CreateCursorBehindTailProblemDetails(context.Request.Path, cursorEx),
                    ValidationException => CreateValidationProblemDetails(context.Request.Path, ex),
                    _ => CreateProblemDetails(context.Request.Path, context.TraceIdentifier, ex),
                };

                if (problemDetails.Status.HasValue)
                {
                    ClearResponse(context, problemDetails.Status.Value);
                }

                await WriteProblemDetails(context, problemDetails);

                return;
            }
            catch (Exception inner)
            {
                _logger.LogError(inner, "Unexpected error. Failed to create problem details.");
            }

            throw;
        }
    }

    private static void ClearResponse(HttpContext context, int statusCode)
    {
        var headers = new HeaderDictionary();

        // Make sure problem responses are never cached.
        headers.Append(HeaderNames.CacheControl, "no-cache, no-store, must-revalidate, max-age=0");
        headers.Append(HeaderNames.Pragma, "no-cache");
        headers.Append(HeaderNames.Expires, "0");

        foreach (var header in context.Response.Headers)
        {
            // Because the CORS middleware adds all the headers early in the pipeline,
            // we want to copy over the existing Access-Control-* headers after resetting the response.
            if (_corsHeaderNames.Contains(header.Key))
            {
                headers.Add(header);
            }
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        foreach (var header in headers)
        {
            context.Response.Headers.Add(header);
        }
    }

    private static string GetTitle(Exception ex, string fallback)
    {
        return !string.IsNullOrEmpty(ex.Message) ? ex.Message : fallback;
    }

    private Task WriteProblemDetails(HttpContext context, ProblemDetails details)
    {
        var routeData = context.GetRouteData() ?? _emptyRouteData;

        var actionContext = new ActionContext(context, routeData, _emptyActionDescriptor);

        var result = new ObjectResult(details)
        {
            StatusCode = details.Status ?? context.Response.StatusCode,
            DeclaredType = details.GetType(),
        };

        result.ContentTypes.Add("application/problem+json");
        result.ContentTypes.Add("application/problem+xml");

        return _executor.ExecuteAsync(actionContext, result);
    }

    private DefaultProblemDetails CreateCursorBehindTailProblemDetails(string instance, CursorBehindTailException ex)
    {
        return new DefaultProblemDetails
        {
            Instance = instance,
            Status = StatusCodes.Status409Conflict,
            Title = ex.Message,
            ErrorCode = "CURSOR_BEHIND_TAIL",
        };
    }

    private DefaultValidationProblemDetails CreateValidationProblemDetails(string instance, Exception exception)
    {
        var errors = (exception as ValidationException)?.Errors;

        var modelState = new ModelStateDictionary();
        if (errors != null && errors.Any())
        {
            foreach (var error in errors)
            {
                error.Value?.ToList().ForEach(
                    x => modelState.AddModelError(error.Key, x));
            }
        }

        return new DefaultValidationProblemDetails(modelState)
        {
            Instance = instance,
            Status = StatusCodes.Status400BadRequest,
            Title = GetTitle(exception, "Validation error"),
            Detail = GetDetail(exception),
            ErrorCode = (exception as BaseException)?.ErrorCode,
        };
    }

    private ProblemDetails CreateProblemDetails(string instance, string traceId, Exception exception)
    {
        switch (exception)
        {
            case ItemExistsException ex:
                return new DefaultProblemDetails
                {
                    Instance = instance,
                    Status = StatusCodes.Status409Conflict,
                    Title = GetTitle(ex, "Item already exists"),
                    Detail = GetDetail(ex),
                };

            case ItemNotFoundException ex:
                return new DefaultProblemDetails
                {
                    Instance = instance,
                    Status = StatusCodes.Status404NotFound,
                    Title = GetTitle(ex, "Item not found"),
                    Detail = GetDetail(ex),
                };

            case ArgumentException ex:
                return new DefaultValidationProblemDetails(new Dictionary<string, string[]>
                {
                    {
                        ex.ParamName ?? "Parameter", [GetTitle(exception, "Argument is invalid")]
                    },
                })
                {
                    Instance = instance,
                    Status = StatusCodes.Status400BadRequest,
                    Title = GetTitle(exception, "Argument is invalid"),
                    Detail = GetDetail(ex),
                };

            default:
                _logger.LogError($"{exception}. TraceId: {traceId}");

                return new UnexpectedProblemDetails
                {
                    Instance = instance,
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Unexpected Error",
                    Timestamp = DateTimeOffset.UtcNow,
                    TraceId = traceId,
                    Detail = GetDetail(exception),
                };
        }
    }

    private string? GetDetail(Exception ex)
    {
        return _environment.IsDevelopment()
            ? ex.ToString()
            : null;
    }
}
