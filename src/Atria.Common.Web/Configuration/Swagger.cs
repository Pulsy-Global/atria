using Atria.Common.Models.Generic;
using Atria.Common.Web.Swagger.Constants;
using Atria.Common.Web.Swagger.Extensions;
using Atria.Common.Web.Swagger.Helpers;
using Atria.Common.Web.Swagger.Models;
using Atria.Common.Web.Swagger.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics;
using System.Reflection;

namespace Atria.Common.Web.Configuration;

public static class Swagger
{
    private const string SwaggerApiTitle = "Atria API";

    private static readonly List<Type> _customGenericTypes = new()
    {
        typeof(PagedList<>),
    };

    private static List<SwaggerDoc> _docs = new()
    {
        new SwaggerDoc
        {
            Version = "v1",
            Title = SwaggerArea.MainApi,
            Area = SwaggerArea.MainApi,
        },
    };

    public static void AddSwaggerServices(this IServiceCollection services, IConfiguration configuration)
    {
        var enabledDocs = GetEnabledSwaggerDocs(configuration);
        var isDebugSession = Debugger.IsAttached;

        services.AddSwaggerWithApiPrefix();

        services.AddCommonSwaggerServices(new SwaggerServicesOptions
        {
            CustomGenericTypes = _customGenericTypes,
            Docs = enabledDocs,
        });
    }

    public static void AddSwaggerWithApiPrefix(this IServiceCollection services)
    {
        services.ConfigureSwaggerGen(options =>
        {
            options.AddServer(new OpenApiServer
            {
                Url = "/api",
            });
        });
    }

    public static void AddCommonSwaggerServices(this IServiceCollection services, SwaggerServicesOptions servicesOptions)
    {
        services.AddSwaggerGen();

        services.AddCachedSwaggerGen(options =>
        {
            foreach (var doc in servicesOptions.Docs)
            {
                options.SwaggerDoc(doc.Area, new OpenApiInfo
                {
                    Title = doc.Title,
                    Version = doc.Version,
                });
            }

            options.DocInclusionPredicate((documentName, apiDescription) =>
            {
                if (!apiDescription.TryGetMethodInfo(out _))
                {
                    return false;
                }

                string? docArea = null;
                var docGroupName = documentName;

                if (documentName.Contains("-"))
                {
                    var parts = documentName.Split("-");
                    docArea = parts[0];
                    docGroupName = parts[1];
                }

                apiDescription.ActionDescriptor.RouteValues
                    .TryGetValue("area", out var area);

                return area == docArea && (apiDescription.GroupName == null ||
                                           apiDescription.GroupName == docGroupName);
            });

            var xmlFile = $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            options.IncludeXmlComments(xmlPath, true);

            options.CustomSchemaIds(SchemaIdSelector.CustomSchemaIdFactory(servicesOptions.CustomGenericTypes));

            options.CustomOperationIds(apiDesc =>
            {
                var actionDescriptor = apiDesc.ActionDescriptor
                    as ControllerActionDescriptor;

                return actionDescriptor?.ActionName;
            });

            options.ExcludeProperty<ProblemDetails>(x => x.Extensions);

            options.MapType<Stream>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "binary",
            });

            options.CustomSchemaIds(type =>
            {
                var schemaId = SchemaIdSelector.CustomSchemaIdFactory(
                    servicesOptions.CustomGenericTypes)(type);

                return schemaId?
                    .Replace("[", " <")
                    .Replace("]", "> ");
            });

            servicesOptions.AdditionalOptions?.Invoke(options);
        });
    }

    public static void UseSwaggerMiddleware(this IApplicationBuilder app, IConfiguration configuration)
    {
        var enabledDocs = GetEnabledSwaggerDocs(configuration);

        app.UseSwaggerMiddleware(new SwaggerMiddlewareOptions
        {
            Docs = enabledDocs,
        });

        app.Map("/openapi.json", appBuilder =>
        {
            appBuilder.Run(async context =>
            {
                context.Response.Redirect($"/swagger/{SwaggerArea.MainApi}/swagger.json");
            });
        });
    }

    public static void UseSwaggerMiddleware(this IApplicationBuilder app, SwaggerMiddlewareOptions middlewareOptions)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.ShowCommonExtensions();

            foreach (var doc in middlewareOptions.Docs)
            {
                c.SwaggerEndpoint($"./{doc.Area}/swagger.json", doc.Title);
            }
        });
    }

    private static List<SwaggerDoc> GetEnabledSwaggerDocs(IConfiguration configuration)
    {
        var enabledAreas = configuration
            .GetSection("Swagger")
            .GetSection("EnabledAreas")
            .Get<List<string>>() ?? [];

        var isDebugSession = Debugger.IsAttached;

        return isDebugSession ? _docs : _docs.Where(x => enabledAreas.Contains(x.Area)).ToList();
    }
}
