using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Concurrent;

namespace Atria.Common.Web.Swagger.Providers;

public class CachedSwaggerProvider : SwaggerGenerator, ISwaggerProvider
{
    private static readonly ConcurrentDictionary<string, OpenApiDocument> _cache = new();

    public CachedSwaggerProvider(
        IApiDescriptionGroupCollectionProvider apiDescriptionsProvider,
        ISchemaGenerator schemaGenerator,
        IOptions<SwaggerGeneratorOptions> optionsAccessor)
        : base(optionsAccessor.Value, apiDescriptionsProvider, schemaGenerator)
    {
    }

    /// <inheritdoc />
    public new OpenApiDocument GetSwagger(string documentName, string? host = null, string? basePath = null)
    {
        var cacheKey = $"{documentName}_{host}_{basePath}";

        return _cache.GetOrAdd(cacheKey, key => base.GetSwagger(documentName, host, basePath));
    }
}
