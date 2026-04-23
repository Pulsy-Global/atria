using Mapster;
using Mapster.Models;
using MapsterMapper;
using System.Linq.Expressions;
using System.Reflection;

namespace Atria.Common.Web.OData.Helpers;

public static class ODataMapsterMappingHelper
{
    public static Dictionary<string, string> GetPropertyMappingsFromMapster(Type sourceType, Type targetType, IMapper? mapper)
    {
        var mappings = new Dictionary<string, string>();

        try
        {
            TypeAdapterConfig? config = null;

            if (mapper is ServiceMapper serviceMapper)
            {
                config = serviceMapper.Config;
            }

            if (config == null && TypeAdapterConfig.GlobalSettings.RuleMap.Any())
            {
                config = TypeAdapterConfig.GlobalSettings;
            }

            if (config != null)
            {
                var typeKey = new TypeTuple(sourceType, targetType);

                if (config.RuleMap.TryGetValue(typeKey, out var rule))
                {
                    if (rule.Settings?.Resolvers != null)
                    {
                        foreach (var resolver in rule.Settings.Resolvers)
                        {
                            if (!string.IsNullOrEmpty(resolver.DestinationMemberName))
                            {
                                var sourcePropertyName = GetPropertyNameFromResolver(resolver);
                                var targetPropertyName = resolver.DestinationMemberName;

                                if (!string.IsNullOrEmpty(sourcePropertyName) && !string.IsNullOrEmpty(targetPropertyName))
                                {
                                    mappings[sourcePropertyName] = targetPropertyName;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting Mapster mappings: {ex.Message}");
        }

        return mappings;
    }

    public static string? GetPropertyNameFromResolver(object resolver)
    {
        try
        {
            var resolverType = resolver.GetType();

            var memberProperty = resolverType.GetProperty("MemberName");

            if (memberProperty?.GetValue(resolver) is string memberName)
            {
                return memberName;
            }

            var allProperties = resolverType.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var prop in allProperties)
            {
                var value = prop.GetValue(resolver);

                if (value is LambdaExpression lambda)
                {
                    var propName = GetPropertyNameFromExpression(lambda);

                    if (!string.IsNullOrEmpty(propName))
                    {
                        return propName;
                    }
                }
            }

            var allFields = resolverType.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in allFields)
            {
                var value = field.GetValue(resolver);

                if (value is LambdaExpression lambda)
                {
                    var propName = GetPropertyNameFromExpression(lambda);

                    if (!string.IsNullOrEmpty(propName))
                    {
                        return propName;
                    }
                }
            }

            var invokerProperty = resolverType.GetProperty("Invoker");

            if (invokerProperty?.GetValue(resolver) is object invoker)
            {
                var invokerType = invoker.GetType();

                var expressionProperty = invokerType.GetProperty("Expression") ??
                                         invokerType.GetProperty("Lambda") ??
                                         invokerType.GetProperty("Func");

                if (expressionProperty?.GetValue(invoker) is LambdaExpression lambda)
                {
                    return GetPropertyNameFromExpression(lambda);
                }
            }
        }
        catch
        {
        }

        return null;
    }

    public static string? GetPropertyNameFromExpression(LambdaExpression? expression)
    {
        if (expression?.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        return null;
    }
}
