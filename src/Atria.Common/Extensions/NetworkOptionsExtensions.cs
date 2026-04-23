using Atria.Common.Models.Options;

namespace Atria.Common.Extensions;

public static class NetworksConfigExtensions
{
    public static NetworkOptions GetNetworkOptionsById(
        this NetworksConfig networksConfiguration,
        string networkId)
    {
        foreach (var group in networksConfiguration.Networks.Values)
        {
            var option = group.Environments.FirstOrDefault(env => env.Id == networkId);

            if (option != null)
            {
                return option;
            }
        }

        throw new ArgumentException($"Network {networkId} not found");
    }
}
