using Atria.Common.Extensions;
using Atria.Core.Data.Context;
using Atria.Core.Data.Context.Handlers.Abstractions;
using Atria.Core.Data.Repositories.Context;
using Atria.Core.Data.Repositories.Context.Interfaces;
using Atria.Core.Data.UnitOfWork.Factory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atria.Core.Data.Extensions;

public static class ServiceExtensions
{
    public static void AddEntityFramework(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEntityFrameworkContext(configuration);
        services.AddEntityFrameworkRepositories();
        services.AddEntityFrameworkHandlers();
    }

    private static void AddEntityFrameworkContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AtriaDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("AtriaConnection"), (builder) =>
            {
                builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                builder.CommandTimeout(100);
            });

            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });
    }

    private static void AddEntityFrameworkRepositories(this IServiceCollection services)
    {
        services.AddTransient<IUnitOfWorkFactory, UnitOfWorkFactory>();
        services.AddTransient<IFeedRepository, FeedRepository>();
        services.AddTransient<IDeployRepository, DeployRepository>();
        services.AddTransient<IFeedOutputRepository, FeedOutputRepository>();
        services.AddTransient<IFeedTagRepository, FeedTagRepository>();
        services.AddTransient<IOutputRepository, OutputRepository>();
        services.AddTransient<IOutputTagRepository, OutputTagRepository>();
        services.AddTransient<ITagRepository, TagRepository>();
    }

    private static void AddEntityFrameworkHandlers(this IServiceCollection services)
    {
        services.RegisterAllTypes<ISaveChangesHandler>(
            typeof(AtriaDbContext).Assembly,
            ServiceLifetime.Singleton);
    }
}
