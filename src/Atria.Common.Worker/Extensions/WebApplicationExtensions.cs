using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Atria.Common.Worker.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapWorkerHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = _ => true,
        });

        app.MapHealthChecks("/health");

        return app;
    }
}
