using Atria.Business.Extensions;
using Atria.Common.Extensions;
using Atria.Common.KV.Extensions;
using Atria.Common.Web.Configuration;
using Atria.Core.Api.Extensions;
using Atria.Core.Business.Extensions;
using Atria.Core.Data.Extensions;
using Atria.Orchestrator.Extension;
using Atria.Pipeline.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureOptionFiles();
builder.AddSerilogLogging(builder.Configuration);
builder.ConfigureKestrel(builder.Configuration);

builder.Services.AddVersioningApi();
builder.Services.AddMvcServices();
builder.Services.ConfigureApiBehavior();
builder.Services.AddApiServices();
builder.Services.AddCorsServices(builder.Configuration);
builder.Services.AddSwaggerServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();

builder.Services.AddBlockProvider(builder.Configuration);
builder.Services.AddBusinessServices(builder.Configuration);
builder.Services.AddApiBusinessServices(builder.Configuration);
builder.Services.AddEkvServices(builder.Configuration);
builder.Services.AddEntityFramework(builder.Configuration);

builder.Services.AddOrchestratorService(builder.Configuration);
builder.Services.AddMapsterService();
builder.Services.AddHealthCheckServices(builder.Configuration);

var app = builder.Build();

app.MigrateDatabases(builder.Configuration);
app.UseSwaggerMiddleware(builder.Configuration);
app.UseSecurityMiddleware();
app.UseRouting();
app.ConfigureCoreEndpoints();

app.Run();
