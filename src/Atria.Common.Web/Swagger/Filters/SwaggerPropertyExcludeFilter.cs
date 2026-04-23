using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq.Expressions;
using System.Reflection;

namespace Atria.Common.Web.Swagger.Filters;

public class SwaggerPropertyExcludeFilter<T> : ISchemaFilter
{
    private readonly Type _type;
    private readonly Expression<Func<T, object>> _propSelector;

    public SwaggerPropertyExcludeFilter(Expression<Func<T, object>> propSelector)
    {
        _type = typeof(T);
        _propSelector = propSelector;
    }

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema?.Properties == null || !schema.Properties.Any())
        {
            return;
        }

        var excludedProperty = _type.GetProperties()
            .FirstOrDefault(x => x.Name == GetPropertyInfo().Name);

        if (excludedProperty != null && schema.Properties.ContainsKey(excludedProperty.Name.ToLower()))
        {
            schema.Properties.Remove(excludedProperty.Name.ToLower());
        }
    }

    public PropertyInfo GetPropertyInfo()
    {
        if (!(_propSelector.Body is MemberExpression memberExpr))
        {
            throw new ArgumentException(string.Format(
                "Expression '{0}' refers to a method, not a property.",
                _propSelector.ToString()));
        }

        var propInfo = memberExpr.Member as PropertyInfo;
        if (propInfo == null)
        {
            throw new ArgumentException(string.Format(
                "Expression '{0}' refers to a field, not a property.",
                memberExpr.ToString()));
        }

        if (propInfo.ReflectedType != null &&
            _type != propInfo.ReflectedType &&
            !_type.IsSubclassOf(propInfo.ReflectedType))
        {
            throw new ArgumentException(string.Format(
                "Expression '{0}' refers to a property that is not from type {1}.",
                memberExpr.ToString(),
                _type));
        }

        return propInfo;
    }
}
