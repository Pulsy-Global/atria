using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Atria.Core.Spa.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(
            "/health/live",
            new HealthCheckOptions
            {
                Predicate = _ => false,
            });

        app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions
            {
                Predicate = _ => false,
            });

        app.MapHealthChecks("/health");

        return app;
    }
}
