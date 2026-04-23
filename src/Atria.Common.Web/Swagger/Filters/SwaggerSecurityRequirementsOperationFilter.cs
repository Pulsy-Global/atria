using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Atria.Common.Web.Swagger.Filters;

public class SwaggerSecurityRequirementsOperationFilter : IOperationFilter
{
    private readonly string _audience;

    public SwaggerSecurityRequirementsOperationFilter(string audience)
    {
        _audience = audience;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.ApiDescription.TryGetMethodInfo(out var methodInfo))
        {
            return;
        }
    }
}
