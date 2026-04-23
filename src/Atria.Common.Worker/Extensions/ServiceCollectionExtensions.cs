using Atria.Common.Messaging.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace Atria.Common.Worker.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNatsHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var messagingSettings = new MessagingSettings();
        configuration.GetSection("Messaging").Bind(messagingSettings);

        var natsOpts = new NatsOpts
        {
            Url = messagingSettings.Url,
            AuthOpts = new NatsAuthOpts
            {
                Username = messagingSettings.Username,
                Password = messagingSettings.Password,
            },
        };

        var connectionFactory = new Func<IServiceProvider, INatsConnection>(_ => new NatsConnection(natsOpts));

        services
            .AddHealthChecks()
            .AddNats(connectionFactory);

        return services;
    }
}
