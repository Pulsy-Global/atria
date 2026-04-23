using Atria.Common.KV.Extensions;
using Atria.Feed.Runtime.Configuration.Options;
using Atria.Feed.Runtime.Engine;
using Atria.Feed.Runtime.Engine.Filters.Js;
using Atria.Feed.Runtime.Engine.Filters.Js.Interfaces;
using Atria.Feed.Runtime.Engine.Filters.Js.Options;
using Atria.Feed.Runtime.Processing;
using Atria.Feed.Runtime.Services;
using Atria.Pipeline.Options;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pulsy.EKV.Client.Configuration;

namespace Atria.Feed.Runtime.Extensions;

public static class RuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RuntimeOptions>(configuration.GetSection("Runtime"));
        services.Configure<JsRuntimeOptions>(configuration.GetSection("Runtime:JsRuntime"));
        services.Configure<LeaseOptions>(configuration.GetSection(LeaseOptions.SectionName));

        services.AddEkv(configuration);

        services.AddSingleton<IJsModuleRegistry, JsModuleRegistry>();
        services.AddSingleton<IJsRuntimeProvider, JsRuntimeProvider>();

        services.AddSingleton<FeedManager>();
        services.AddSingleton<FeedRuntimeRegistry>();
        services.AddSingleton<FeedBlockProcessor>();
        services.AddSingleton<FeedLifecycleManager>();
        services.AddSingleton<LeaseStore>();
        services.AddHostedService<FeedRuntimeService>();
        services.AddHostedService<FeedTestService>();
        services.AddFission(configuration);

        return services;
    }

    private static void AddEkv(this IServiceCollection services, IConfiguration configuration)
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
}
