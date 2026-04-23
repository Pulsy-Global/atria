using Atria.Common.Extensions;
using Atria.Common.Web.Models.Abstractions;
using Atria.Common.Web.Models.Errors;
using Atria.Common.Web.OData.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Atria.Common.Web.Configuration;

public static class Framework
{
    public static void AddMvcServices(this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddNewtonsoftJson(SetupMvcNewtonsoftJson)
            .AddDotNetOData();

        services.AddMvc();
    }

    public static void AddVersioningApi(this IServiceCollection services)
    {
        services.AddApiVersioning(SetupApiVersioning);
        services.AddVersionedApiExplorer(SetupVersionedApiExplorer);
    }

    public static void ConfigureApiBehavior(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new DefaultValidationProblemDetails(context.ModelState)
                {
                    Instance = context.HttpContext.Request.Path,
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation error",
                    Detail = "Please refer to the errors property for additional details.",
                };

                return new BadRequestObjectResult(problemDetails)
                {
                    ContentTypes = { "application/problem+json", "application/problem+xml" },
                };
            };
        });
    }

    private static void SetupMvcNewtonsoftJson(MvcNewtonsoftJsonOptions options)
    {
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy(),
        };
    }

    private static void SetupVersionedApiExplorer(ApiExplorerOptions options)
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    }

    private static void SetupApiVersioning(ApiVersioningOptions options)
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader());
    }

    private static void AddDotNetOData(this IMvcBuilder builder)
    {
        builder
            .AddOData(options =>
            {
                options.EnableQueryFeatures();
                options.TimeZone = TimeZoneInfo.Utc;

                options.AddRouteComponents("v{version}", ConfigureEdmModel(), container =>
                {
                    container.AddSingleton<ODataUriResolver>(_ => new ODataEnumResolver
                    {
                        EnableCaseInsensitive = true,
                    });
                });
            })
            .AddViewLocalization();

        ConfigureFormatters(builder);
    }

    private static void ConfigureFormatters(IMvcBuilder builder)
    {
        builder.AddMvcOptions(options =>
        {
            foreach (var outputFormatter in options.OutputFormatters
                         .OfType<ODataOutputFormatter>()
                         .Where(f => f.SupportedMediaTypes.Count == 0))
            {
                outputFormatter.SupportedMediaTypes.Add(
                    new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
            }

            foreach (var inputFormatter in options.InputFormatters
                         .OfType<ODataInputFormatter>()
                         .Where(f => f.SupportedMediaTypes.Count == 0))
            {
                inputFormatter.SupportedMediaTypes.Add(
                    new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
            }
        });
    }

    private static IEdmModel ConfigureEdmModel()
    {
        var builder = new ODataConventionModelBuilder();

        var types = Assembly.GetEntryAssembly()?
            .GetTypesImplementedInterface<IODataDto>();

        if (types != null)
        {
            foreach (var type in types)
            {
                var entityType = builder.AddEntityType(type);
                builder.AddEntitySet(type.Name, entityType);
            }
        }

        return builder.GetEdmModel();
    }
}
