using Atria.Feed.Ingestor.ChainClients.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using System.Net;

namespace Atria.Feed.Ingestor.ChainClients;

public class EvmRetryService : IEvmRetryService
{
    private readonly ILogger<EvmRetryService> _logger;
    private readonly IAsyncPolicy _combinedPolicy;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly IAsyncPolicy _circuitBreakerPolicy;
    private int _rateLimitHits;

    public EvmRetryService(ILogger<EvmRetryService> logger)
    {
        _logger = logger;
        _retryPolicy = CreateRetryPolicy();
        _circuitBreakerPolicy = CreateCircuitBreakerPolicy();
        _combinedPolicy = Policy.WrapAsync(_retryPolicy, _circuitBreakerPolicy);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
    {
        return await _combinedPolicy.ExecuteAsync(
            async _ => await operation(),
            new Context(operationName));
    }

    private static bool IsRateLimitException(Exception exception)
    {
        if (exception is HttpRequestException httpEx &&
            httpEx.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return true;
        }

        var message = exception.Message;
        return message?.Contains("429", StringComparison.OrdinalIgnoreCase) == true
            || message?.Contains("rate limit", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static TimeSpan CalculateRateLimitDelay(int retryAttempt)
    {
        var baseDelay = TimeSpan.FromMilliseconds(500 * retryAttempt);
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));
        return baseDelay + jitter;
    }

    private IAsyncPolicy CreateRetryPolicy()
    {
        var rateLimitRetryPolicy = Policy
            .Handle<Exception>(IsRateLimitException)
            .WaitAndRetryAsync(
                retryCount: 15,
                sleepDurationProvider: retryAttempt => CalculateRateLimitDelay(retryAttempt),
                onRetry: (exception, delay, retryCount, context) =>
                {
                    Interlocked.Increment(ref _rateLimitHits);

                    _logger.LogWarning(
                        "Rate limit hit for {Operation}: {Error}. Retry {RetryCount}/{MaxRetries} after {Delay:F1}s.",
                        context.OperationKey,
                        exception.Message,
                        retryCount,
                        delay.TotalSeconds,
                        _rateLimitHits);
                });

        var standardRetryPolicy = Policy
            .Handle<OperationCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested)
            .Or<Exception>(ex => !(ex is OperationCanceledException || IsRateLimitException(ex)))
            .WaitAndRetryAsync(
                retryCount: 10,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryAttempt - 1), 30)),
                onRetry: (exception, delay, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Attempt {RetryCount} failed for {Operation}: {Error}. Retrying in {Delay:F1}s",
                        retryCount,
                        context.OperationKey,
                        exception.Message,
                        delay.TotalSeconds);
                });

        return Policy.WrapAsync(rateLimitRetryPolicy, standardRetryPolicy);
    }

    private AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy()
    {
        return Policy
            .Handle<Exception>(IsRateLimitException)
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 10,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, breakDelay) =>
                {
                    _logger.LogWarning(
                        "Circuit breaker opened for {BreakDelay:F1}m due to persistent rate limiting: {Error}",
                        breakDelay.TotalMinutes,
                        exception.Message);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset. Resuming normal request rate.");
                });
    }
}
