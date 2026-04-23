using Atria.Core.Business.Models.Dto.Network;

namespace Atria.Core.Business.Managers.Interfaces;

public interface IConfigManager : IBaseManager
{
    NetworksDto GetNetworks();

    Task<LatestBlockDto> GetLatestBlockAsync(string networkId);
}
