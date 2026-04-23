using Atria.Common.Extensions;
using Atria.Common.Models.Options;
using Atria.Feed.Ingestor.ChainClients;
using Atria.Feed.Ingestor.ChainClients.Interfaces;
using Atria.Feed.Ingestor.Config.Options;
using Atria.Feed.Ingestor.Services.RealtimePublisher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atria.Feed.Ingestor.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureIngestorOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var ingestorSection = configuration.GetSection("Ingestor");

        services.AddOptions<IngestorOptions>().Bind(ingestorSection);

        services.Configure<IngestorNetworkOptions>(options =>
        {
            var ingestorOptions = ingestorSection.Get<IngestorOptions>();
            var networksConfig = configuration.Get<NetworksConfig>();

            if (ingestorOptions == null)
            {
                throw new ArgumentNullException(nameof(ingestorOptions));
            }

            options.NetworkOptions = networksConfig!.GetNetworkOptionsById(ingestorOptions.NetworkId);
        });
    }

    public static void AddNodeClient(this IServiceCollection services)
    {
        services.AddTransient<ReorgHeaderHandler>();

        services.AddHttpClient(EvmClientFactory.HttpClientName)
            .AddHttpMessageHandler<ReorgHeaderHandler>();

        services.AddSingleton<IClientFactory<EvmClient>, EvmClientFactory>();
        services.AddSingleton<EvmRetryService>();

        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            MaxDepth = 256,
        };
    }

    public static void AddRealtimePublisher(this IServiceCollection services)
    {
        services.AddSingleton<IEvmWebSocketClient>(sp =>
        {
            var networkOptions = sp.GetRequiredService<IOptions<IngestorNetworkOptions>>();
            var logger = sp.GetRequiredService<ILogger<EvmWebSocketClient>>();
            return new EvmWebSocketClient(logger, networkOptions.Value.NetworkOptions);
        });

        services.AddSingleton<BlockProcessor>();
        services.AddSingleton<ServiceStateManager>();

        services.AddHostedService<RealtimePublisher>();
    }
}
