using Atria.Feed.Ingestor.ChainClients.Interfaces;
using Atria.Feed.Ingestor.ChainClients.Models;
using Atria.Feed.Ingestor.Observability;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Numerics;

namespace Atria.Feed.Ingestor.ChainClients;

public class EvmClient
{
    private readonly IEvmHttpClient _httpClient;
    private readonly IEvmRetryService _retryService;
    private readonly ILogger<EvmClient> _logger;
    private readonly IngestorMetricsRecorder _metrics;

    public EvmClient(
        ILogger<EvmClient> logger,
        IEvmHttpClient httpClient,
        IEvmRetryService retryService,
        IngestorMetricsRecorder metrics)
    {
        _httpClient = httpClient;
        _retryService = retryService;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<BlockData> GetByBlockNumberAsync(BigInteger blockNumber, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting block data for {BlockNumber}", blockNumber);

        return await ExecuteFetchAsync(
            () => _retryService.ExecuteAsync(
                () => _httpClient.FetchBlockAllDataAsync(blockNumber, ct),
                IngestorMetricsRecorder.BlockDataOperation),
            IngestorMetricsRecorder.BlockDataOperation);
    }

    public async Task<BigInteger> GetLatestBlockNumberAsync(CancellationToken ct = default)
    {
        return await ExecuteFetchAsync(
            () => _retryService.ExecuteAsync(
                () => _httpClient.GetLatestBlockNumberAsync(ct),
                IngestorMetricsRecorder.ChainHeadOperation),
            IngestorMetricsRecorder.ChainHeadOperation);
    }

    private async Task<T> ExecuteFetchAsync<T>(Func<Task<T>> fetch, string operation)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var succeeded = false;
        _metrics.FetchStarted();

        try
        {
            var result = await fetch();
            succeeded = true;

            return result;
        }
        finally
        {
            _metrics.FetchCompleted(operation, succeeded, Stopwatch.GetElapsedTime(startedAt));
        }
    }
}
