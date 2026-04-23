using Atria.Common.Worker.Extensions;
using Atria.Core.Data.Context;

namespace Atria.Core.Api.Extensions;

public static class CoreEndpointsExtensions
{
    public static void ConfigureCoreEndpoints(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGroup("/api").MapControllers();

            endpoints.MapHealthChecks("/health/live");
            endpoints.MapHealthChecks("/health/ready");
            endpoints.MapHealthChecks("/health");
        });
    }

    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<AtriaDbContext>();

        services.AddNatsHealthChecks(configuration);

        return services;
    }
}
