using Atria.Common.Web.Models.Options;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Interfaces;
using Atria.Feed.Runtime.Engine.Functions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atria.Feed.Runtime.Extensions;

public static class FissionServiceCollectionExtensions
{
    public static IServiceCollection AddFissionClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FissionClientOptions>(configuration.GetSection("Runtime:Functions:Fission:Client"));

        services.AddHttpClient("FissionClient", (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FissionClientOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.HttpClientTimeoutSeconds);
        });

        services.AddScoped<IFissionClient, FissionClient>();

        return services;
    }

    public static IServiceCollection AddFission(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FissionOptions>(configuration.GetSection("Runtime:Functions:Fission"));
        services.Configure<FeaturesOptions>(configuration.GetSection("Features"));

        services.AddFissionClient(configuration);

        return services;
    }
}
