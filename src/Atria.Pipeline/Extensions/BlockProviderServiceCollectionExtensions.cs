using Atria.Pipeline.Interfaces;
using Atria.Pipeline.Messaging;
using Atria.Pipeline.Options;
using Atria.Pipeline.Providers;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atria.Pipeline.Extensions;

public static class BlockProviderServiceCollectionExtensions
{
    public static IServiceCollection AddBlockProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BlockProviderOptions>(
            configuration.GetSection(BlockProviderOptions.SectionName));

        services.Configure<LeaseOptions>(
            configuration.GetSection(LeaseOptions.SectionName));

        services.AddSingleton<BlockKvStore>();
        services.AddSingleton<ChainStateStore>();
        services.AddSingleton<IFeedCursorStore, FeedCursorStore>();
        services.AddSingleton<LeaseStore>();
        services.AddSingleton<IBlockProvider, KvBlockProvider>();
        services.AddSingleton<IFeedPublisher, FeedPublisher>();
        services.AddSingleton<IFeedSubscriber, FeedSubscriber>();

        return services;
    }
}
