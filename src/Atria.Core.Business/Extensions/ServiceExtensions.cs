using Atria.Common.KV.Extensions;
using Atria.Common.Net;
using Atria.Core.Business.Facades;
using Atria.Core.Business.Managers;
using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Options;
using Atria.Core.Business.Services.Metrics;
using Atria.Core.Business.Services.Probe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Atria.Core.Business.Extensions;

public static class ServiceExtensions
{
    public static void AddApiBusinessServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTransient<FeedFacade>();
        services.AddTransient<OutputFacade>();
        services.AddTransient<ConfigFacade>();
        services.AddTransient<TagFacade>();
        services.AddTransient<KvFacade>();
        services.AddTransient<FeedMetricsFacade>();

        services.AddTransient<IFeedManager, FeedManager>();
        services.AddTransient<IDeployManager, DeployManager>();
        services.AddTransient<IOutputManager, OutputManager>();
        services.AddTransient<IConfigManager, ConfigManager>();
        services.AddTransient<ITagManager, TagManager>();
        services.AddTransient<IKvManager, KvManager>();

        services.AddEkvServices(configuration);

        services.Configure<FeedMetricsOptions>(configuration.GetSection(FeedMetricsOptions.SectionName));
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient<IFeedBusinessMetricsStore, VictoriaMetricsFeedBusinessMetricsStore>();
        services.AddTransient<IFeedMetricsQueryService, FeedMetricsQueryService>();

        AddSsrfGuard(services, configuration);
        AddWebhookProbe(services);
    }

    private static void AddSsrfGuard(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SsrfGuardOptions>(configuration.GetSection(SsrfGuardOptions.SectionName));
        services.AddSingleton<SsrfSafeHandlerFactory>();
    }

    private static void AddWebhookProbe(IServiceCollection services)
    {
        services.AddHttpClient<IWebhookProbeService, WebhookProbeService>()
            .ConfigurePrimaryHttpMessageHandler(sp =>
                sp.GetRequiredService<SsrfSafeHandlerFactory>().Create());
    }
}
