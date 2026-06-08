using Atria.Common.Worker.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atria.Common.Worker.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNatsHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;

        services
            .AddHealthChecks()
            .AddCheck<NatsConnectionHealthCheck>("nats");

        return services;
    }
}
