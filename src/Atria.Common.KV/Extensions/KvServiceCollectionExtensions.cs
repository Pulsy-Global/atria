using Atria.Common.KV.Factory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pulsy.EKV.Client;
using Pulsy.EKV.Client.Configuration;

namespace Atria.Common.KV.Extensions;

public static class KvServiceCollectionExtensions
{
    public static void AddEkvServices(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Ekv");
        if (!section.Exists())
        {
            return;
        }

        var options = section.Get<EkvClientOptions>();
        if (options != null)
        {
            services.AddKvStore(options);
        }
    }

    public static IServiceCollection AddKvStore(this IServiceCollection services, EkvClientOptions options)
    {
        services.AddSingleton<IEkvClient>(new EkvClient(options));
        services.AddSingleton<IKvStoreFactory, KvStoreFactory>();

        return services;
    }
}
