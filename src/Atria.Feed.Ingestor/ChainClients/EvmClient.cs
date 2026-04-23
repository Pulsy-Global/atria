using Atria.Feed.Ingestor.ChainClients.Interfaces;
using Atria.Feed.Ingestor.ChainClients.Models;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace Atria.Feed.Ingestor.ChainClients;

public class EvmClient
{
    private readonly IEvmHttpClient _httpClient;
    private readonly IEvmRetryService _retryService;
    private readonly ILogger<EvmClient> _logger;

    public EvmClient(
        ILogger<EvmClient> logger,
        IEvmHttpClient httpClient,
        IEvmRetryService retryService)
    {
        _httpClient = httpClient;
        _retryService = retryService;
        _logger = logger;
    }

    public async Task<BlockData> GetByBlockNumberAsync(BigInteger blockNumber, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting block data for {BlockNumber}", blockNumber);

        return await _retryService.ExecuteAsync(
            () => _httpClient.FetchBlockAllDataAsync(blockNumber, ct),
            $"GetBlockDataAsync for block {blockNumber}");
    }

    public async Task<BigInteger> GetLatestBlockNumberAsync(CancellationToken ct = default)
    {
        return await _retryService.ExecuteAsync(
            () => _httpClient.GetLatestBlockNumberAsync(ct),
            "GetLatestBlockNumberAsync");
    }
}
