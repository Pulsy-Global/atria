using Atria.Common.Exceptions;
using Atria.Common.Messaging.RequestReply;
using Atria.Common.Models.Options;
using Atria.Contracts.Events.Blockchain;
using Atria.Contracts.Subjects.Blockchain;
using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Dto.Network;
using Atria.Core.Data.Entities.Enums;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Numerics;

namespace Atria.Core.Business.Managers;

public class ConfigManager : BaseManager, IConfigManager
{
    private readonly NetworksConfig _networksConfig;
    private readonly IRequestClient _requestClient;

    public ConfigManager(
        ILogger<ConfigManager> logger,
        IMapper mapper,
        IOptions<NetworksConfig> networksConfig,
        IRequestClient requestClient)
        : base(logger, mapper)
    {
        _networksConfig = networksConfig.Value;
        _requestClient = requestClient;
    }

    public NetworksDto GetNetworks()
    {
        var networks = _networksConfig.Networks.Values
            .Where(n => !n.Disabled)
            .Select(n =>
            {
                var enabledEnvironments = n.Environments.Where(e => !e.Disabled).ToList();
                var networkDto = Mapper.Map<NetworkDto>(n);

                networkDto.Environments = enabledEnvironments
                    .Select(env =>
                    {
                        var envDto = Mapper.Map<EnvironmentDto>(env);
                        envDto.AvailableDatasets = GetAvailableDatasets(env.DebugRequestsEnabled);
                        return envDto;
                    })
                    .ToArray();

                return networkDto;
            })
            .ToArray();

        return new NetworksDto { Networks = networks };
    }

    public async Task<LatestBlockDto> GetLatestBlockAsync(string networkId)
    {
        var networkConfig = FindNetworkById(networkId);

        if (networkConfig == null)
        {
            throw new ItemNotFoundException($"Network with ID '{networkId}' not found");
        }

        var subject = Blockchain.Subjects.ChainHeadRequest(networkId);
        var request = new ChainHeadRequest(networkId);

        var response = await _requestClient.SendAsync<ChainHeadRequest, ChainHeadResponse>(
            subject,
            request);

        if (response is null || !response.Success || !response.BlockNumber.HasValue)
        {
            throw new ItemNotFoundException(
                $"Latest block for network '{networkId}' not found. Ensure ingestor is running.");
        }

        return new LatestBlockDto
        {
            NetworkId = networkId,
            BlockNumber = new BigInteger(response.BlockNumber.Value),
        };
    }

    private NetworkOptions? FindNetworkById(string networkId)
    {
        foreach (var group in _networksConfig.Networks.Values)
        {
            if (group.Disabled)
            {
                continue;
            }

            var network = group.Environments
                .FirstOrDefault(e => e.Id == networkId && !e.Disabled);

            if (network != null)
            {
                return network;
            }
        }

        return null;
    }

    private AtriaDataType[] GetAvailableDatasets(bool debugRequestsEnabled)
    {
        var datasets = new List<AtriaDataType>
        {
            AtriaDataType.BlockWithTransactions,
            AtriaDataType.BlockWithLogs,
        };

        if (debugRequestsEnabled)
        {
            datasets.Add(AtriaDataType.BlockWithTraces);
        }

        return datasets.ToArray();
    }
}
