using Atria.Common.Extensions;
using Atria.Common.Messaging.Extensions;
using Atria.Common.Web.Configuration;
using Atria.Common.Worker.Extensions;
using Atria.Feed.Ingestor.Extensions;
using Atria.Feed.Ingestor.Services;
using Atria.Pipeline.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureOptionFiles();
builder.AddSerilogLogging(builder.Configuration);
builder.ConfigureKestrel(builder.Configuration);

builder.Services.AddMapsterService();
builder.Services.ConfigureCommonOptions(builder.Configuration);
builder.Services.ConfigureIngestorOptions(builder.Configuration);
builder.Services.AddMessaging(builder.Configuration);
builder.Services.AddBlockProvider(builder.Configuration);
builder.Services.AddNatsHealthChecks(builder.Configuration);
builder.Services.AddNodeClient();
builder.Services.AddRealtimePublisher();
builder.Services.AddHostedService<DataRequestService>();
builder.Services.AddHostedService<ChainHeadRequestHandler>();

var app = builder.Build();

app.MapWorkerHealthChecks();

app.Run();
