using Atria.Core.Data.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atria.Core.Data.Extensions;

public static class AppExtensions
{
    public static void MigrateDatabases(this IApplicationBuilder app, IConfiguration configuration)
    {
        var autoMigrationsEnabled = configuration.GetValue<bool>("AutoMigrationEnabled");

        if (!autoMigrationsEnabled)
        {
            return;
        }

        using var serviceScope = app.ApplicationServices
            .GetRequiredService<IServiceScopeFactory>()
            .CreateScope();

        serviceScope.ServiceProvider?
            .GetService<AtriaDbContext>()?
            .Database.Migrate();
    }
}
