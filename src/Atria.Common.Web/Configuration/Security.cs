using Atria.Common.Web.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atria.Common.Web.Configuration;

public static class Security
{
    public static void AddCorsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var productionOrigins = (configuration.GetSection("AllowedOrigins").Get<string>() ?? string.Empty).Split(',');

        services.AddCors(options =>
        {
            options.AddPolicy("ProductionCorsPolicy", builder => builder
                .WithOrigins(productionOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
        });
    }

    public static void UseSecurityMiddleware(this IApplicationBuilder app)
    {
        app.UseErrorWrapping();
        app.UseCorsServices();
        app.UseHsts();
    }

    private static void UseCorsServices(this IApplicationBuilder app)
    {
        app.UseCors("ProductionCorsPolicy");
    }

    private static void UseErrorWrapping(this IApplicationBuilder app)
    {
        app.UseMiddleware(typeof(ErrorWrappingMiddleware));
    }
}
