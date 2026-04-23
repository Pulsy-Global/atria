using Atria.Common.Net;
using Atria.Feed.Delivery.Config.Options;
using Atria.Feed.Delivery.FeedPipeline.Handlers.Delivery;
using Atria.Feed.Delivery.FeedPipeline.Interfaces;
using Atria.Feed.Delivery.Services;
using Atria.Feed.Delivery.Services.ServiceHandlers;
using Atria.Pipeline.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atria.Feed.Delivery.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddDeliveryServices(this IServiceCollection svc, IConfiguration configuration)
    {
        svc.AddSingleton<DeliveryConfigService>();
        svc.AddMemoryCache();
        svc.AddBlockProvider(configuration);

        svc.AddScoped<IFeedPipeline, FeedPipeline.FeedPipeline>();
        svc.AddHostedService<FeedDeliveryService>();

        var options = new FeedDeliveryOptions();
        configuration.GetSection(FeedDeliveryOptions.SectionName).Bind(options);
        svc.Configure<FeedDeliveryOptions>(configuration.GetSection(FeedDeliveryOptions.SectionName));

        svc.Configure<SsrfGuardOptions>(configuration.GetSection(SsrfGuardOptions.SectionName));
        svc.AddSingleton<SsrfSafeHandlerFactory>();

        svc.AddHttpClient<WebhookDeliveryHandler>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", options.UserAgent);
        })
        .ConfigurePrimaryHttpMessageHandler(sp =>
            sp.GetRequiredService<SsrfSafeHandlerFactory>().Create());
    }

    public static void AddDeliveryHandlers(this IServiceCollection svc)
    {
        svc.AddHostedService<DeliveryConfigUpdateHandler>();
        svc.AddScoped<IDeliveryHandler>(sp => sp.GetRequiredService<WebhookDeliveryHandler>());
    }
}
