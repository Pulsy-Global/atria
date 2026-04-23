using Atria.Common.Messaging.Core;
using Atria.Common.Messaging.RequestReply;
using Atria.Common.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Atria.Common.Messaging.Extensions;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MessagingSettings>()
            .Bind(configuration.GetSection("Messaging"));

        return services.AddMessagingServices();
    }

    private static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        services.AddSingleton<NatsConnectionManager>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<NatsConnectionManager>());
        services.AddSingleton<StreamManager>();
        services.AddSingleton<IServiceBus, ServiceBus.ServiceBus>();
        services.AddSingleton<IRequestClient, RequestClient>();

        return services;
    }
}
