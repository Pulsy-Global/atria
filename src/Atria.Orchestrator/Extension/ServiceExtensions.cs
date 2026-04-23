using Atria.Orchestrator.Config.Options;
using Atria.Orchestrator.Managers;
using Atria.Orchestrator.Services;
using Atria.Orchestrator.Services.Deployment;
using Atria.Orchestrator.Services.Interfaces;
using Atria.Orchestrator.Services.ServiceHandlers;
using Atria.Pipeline.Options;
using Atria.Pipeline.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atria.Orchestrator.Extension;

public static class ServiceExtensions
{
    public static void AddOrchestratorService(this IServiceCollection services, IConfiguration cfg)
    {
        services.ConfigureOrchestratorOptions(cfg);

        var serviceProvider = services.BuildServiceProvider();
        var orchestratorOptions = serviceProvider.GetRequiredService<IOptions<OrchestratorOptions>>().Value;

        if (!orchestratorOptions.Enabled)
        {
            return;
        }

        services.AddOrchestratorServices(cfg);
        services.AddOrchestratorHandlers();
    }

    private static void AddOrchestratorServices(this IServiceCollection services, IConfiguration cfg)
    {
        services.Configure<LeaseOptions>(cfg.GetSection(LeaseOptions.SectionName));
        services.AddSingleton<LeaseStore>();

        services.AddScoped<IManifestScanner, ManifestScanner>();
        services.AddScoped<IOutputProvisioningManager, OutputProvisioningManager>();
        services.AddScoped<IFeedProvisioningManager, FeedProvisioningManager>();

        services.AddHostedService<ProvisioningService>();
        services.AddHostedService<OrchestratorService>();
    }

    private static void AddOrchestratorHandlers(this IServiceCollection services)
    {
        services.AddHostedService<FeedDeploymentReconciler>();
        services.AddHostedService<DeliveryConfigHandler>();
        services.AddHostedService<FeedPausedHandler>();
        services.AddHostedService<FeedDeployedHandler>();
    }

    private static void ConfigureOrchestratorOptions(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddOptions<OrchestratorOptions>()
            .Bind(cfg.GetSection("Orchestrator"));
    }
}
