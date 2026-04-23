using Atria.Common.Models.Options;
using Atria.Feed.Ingestor.ChainClients;
using Atria.Feed.Ingestor.ChainClients.Interfaces;
using Atria.Feed.Ingestor.Config.Options;
using Atria.Feed.Ingestor.Models;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.Options;

namespace Atria.Feed.Ingestor.Services.RealtimePublisher;

public class ServiceStateManager
{
    private readonly EvmClient _evmClient;
    private readonly ChainStateStore _stateStore;
    private readonly NetworkOptions _chainOptions;

    public ServiceStateManager(
        IClientFactory<EvmClient> evmClientFactory,
        IOptions<IngestorNetworkOptions> networksOptions,
        ChainStateStore stateStore)
    {
        _evmClient = evmClientFactory.CreateClient();
        _stateStore = stateStore;
        _chainOptions = networksOptions.Value.NetworkOptions;
    }

    public async Task<ServiceState> LoadCurrentStateAsync(CancellationToken ct = default)
    {
        var lastHead = await _stateStore.GetHeadAsync(_chainOptions.Id, ct);
        var currentBlock = await _evmClient.GetLatestBlockNumberAsync(ct);

        var lastProcessedBlock = lastHead ?? currentBlock - 1;

        return new ServiceState
        {
            LastProcessedBlock = lastProcessedBlock,
            CurrentChainBlock = currentBlock,
            HasMissedBlocks = currentBlock > lastProcessedBlock + 1,
            MissedBlockCount = currentBlock > lastProcessedBlock + 1 ? currentBlock - lastProcessedBlock - 1 : 0,
        };
    }
}
