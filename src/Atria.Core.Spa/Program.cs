using Atria.Common.Web.Configuration;
using Atria.Core.Spa.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureOptionFiles();

builder.Services.AddSpaServices();
builder.Services.AddMvcServices();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseHttpsRedirection();
app.UseHsts();

app.UseSpaStaticFiles();
app.UseRouting();
app.ConfigureEndpoints();
app.UseSpaFallback();
app.MapHealthChecks();

app.Run();
