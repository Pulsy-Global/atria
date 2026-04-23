using Atria.Common.Web.Swagger.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Atria.Common.Web.Swagger.Extensions;

public static class CachedSwaggerProviderExtensions
{
    public static void AddCachedSwaggerGen(
        this IServiceCollection services,
        Action<SwaggerGenOptions>? setupAction = null)
    {
        services.Replace(ServiceDescriptor.Transient<ISwaggerProvider, CachedSwaggerProvider>());

        if (setupAction != null)
        {
            services.ConfigureSwaggerGen(setupAction);
        }
    }
}
