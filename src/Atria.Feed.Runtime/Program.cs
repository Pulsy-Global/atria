using Atria.Common.Extensions;
using Atria.Common.Messaging.Extensions;
using Atria.Common.Web.Configuration;
using Atria.Common.Worker.Extensions;
using Atria.Feed.Runtime.Extensions;
using Atria.Pipeline.Extensions;
using Microsoft.AspNetCore.Builder;

namespace Atria.Feed.Runtime;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.ConfigureOptionFiles();
        builder.AddSerilogLogging(builder.Configuration);
        builder.ConfigureKestrel(builder.Configuration);

        builder.Services.ConfigureCommonOptions(builder.Configuration);
        builder.Services.AddMessaging(builder.Configuration);
        builder.Services.AddBlockProvider(builder.Configuration);
        builder.Services.AddNatsHealthChecks(builder.Configuration);
        builder.Services.AddRuntime(builder.Configuration);

        var app = builder.Build();

        app.MapWorkerHealthChecks();

        await app.RunAsync();
    }
}
