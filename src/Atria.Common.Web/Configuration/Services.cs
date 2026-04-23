using Atria.Common.Web.Models.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Atria.Common.Web.Configuration;

public static class Services
{
    public static void AddApiServices(this IServiceCollection services)
    {
        services.AddDirectoryBrowser();
        services.AddSwaggerGenNewtonsoftSupport();
        services.AddHttpContextAccessor();
        services.AddEndpointsApiExplorer();
    }

    public static void AddSpaServices(this IServiceCollection services)
    {
        services
            .AddOptions<ConfigurationOptions>()
            .BindConfiguration("Config");
    }
}
