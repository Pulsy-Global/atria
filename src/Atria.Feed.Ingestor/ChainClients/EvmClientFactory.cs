using Atria.Feed.Ingestor.ChainClients.Interfaces;
using Atria.Feed.Ingestor.Config.Options;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atria.Feed.Ingestor.ChainClients;

public class EvmClientFactory : IClientFactory<EvmClient>
{
    public const string HttpClientName = "EvmRpcClient";

    private readonly IOptions<IngestorNetworkOptions> _networkOptions;
    private readonly IMapper _mapper;
    private readonly ILogger<EvmClient> _logger;
    private readonly ILogger<EvmHttpClient> _httpLogger;
    private readonly ILogger<EvmRetryService> _retryLogger;
    private readonly IHttpClientFactory _httpClientFactory;

    public EvmClientFactory(
        IOptions<IngestorNetworkOptions> networkOptions,
        IMapper mapper,
        ILogger<EvmClient> logger,
        ILogger<EvmHttpClient> httpLogger,
        ILogger<EvmRetryService> retryLogger,
        IHttpClientFactory httpClientFactory)
    {
        _networkOptions = networkOptions;
        _mapper = mapper;
        _logger = logger;
        _httpLogger = httpLogger;
        _retryLogger = retryLogger;
        _httpClientFactory = httpClientFactory;
    }

    public EvmClient CreateClient()
    {
        var chainOptions = _networkOptions.Value.NetworkOptions;
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);

        var httpService = new EvmHttpClient(chainOptions, _mapper, _httpLogger, httpClient);
        var retryService = new EvmRetryService(_retryLogger);

        return new EvmClient(
            _logger,
            httpService,
            retryService);
    }
}
