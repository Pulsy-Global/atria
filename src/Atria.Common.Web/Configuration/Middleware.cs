using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Atria.Common.Web.Configuration;

public static class Middleware
{
    public static WebApplication UseSpaStaticFiles(this WebApplication app)
    {
        var spaDistFilePath = Path.Combine(
            app.Environment.WebRootPath,
            "misc",
            "dist",
            "dashboard",
            "browser");

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(spaDistFilePath),
            RequestPath = string.Empty,
        });

        return app;
    }

    public static WebApplication UseSpaFallback(this WebApplication app)
    {
        var spaDistFilePath = Path.Combine(
            app.Environment.WebRootPath,
            "misc",
            "dist",
            "dashboard",
            "browser");

        app.MapFallback(async context =>
        {
            var indexPath = Path.Combine(
                spaDistFilePath,
                "index.html");

            await context.Response.SendFileAsync(indexPath);
        });

        return app;
    }
}
