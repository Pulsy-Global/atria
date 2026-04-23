using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Atria.Common.Web.Configuration;

public static class Endpoints
{
    public static void ConfigureEndpoints(this IApplicationBuilder app)
    {
        app.UseEndpoints(MapEndpoints);
    }

    private static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        endpoints.MapRazorPages();
    }
}
