using Atria.Common.Web.Swagger.Filters;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq.Expressions;

namespace Atria.Common.Web.Swagger.Extensions;

public static class SwaggerGenOptionsExtensions
{
    public static void ExcludeProperty<T>(this SwaggerGenOptions swaggerGenOptions, Expression<Func<T, object>> propSelector)
    {
        swaggerGenOptions.SchemaFilter<SwaggerPropertyExcludeFilter<T>>(propSelector);
    }
}
