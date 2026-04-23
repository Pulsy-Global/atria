using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Dto.Network;

namespace Atria.Core.Business.Facades;

public class ConfigFacade
{
    private readonly IConfigManager _configManager;

    public ConfigFacade(IConfigManager configManager)
    {
        _configManager = configManager;
    }

    public NetworksDto GetNetworks()
    {
        return _configManager.GetNetworks();
    }

    public async Task<LatestBlockDto> GetLatestBlockAsync(string networkId)
    {
        return await _configManager.GetLatestBlockAsync(networkId);
    }
}
