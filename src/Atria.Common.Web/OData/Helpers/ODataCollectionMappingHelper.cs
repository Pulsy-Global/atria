using Atria.Common.Web.OData.Attributes;
using System.Reflection;

namespace Atria.Common.Web.OData.Helpers;

public static class ODataCollectionMappingHelper
{
    private static readonly Lazy<Dictionary<(Type, string), (string, string)>> _globalMappings =
        new(InitializeGlobalMappings);

    public static Dictionary<string, (string CollectionProperty, string ItemProperty)> GetCollectionMappings(Type dtoType)
    {
        var mappings = new Dictionary<string, (string, string)>();

        foreach (var kvp in _globalMappings.Value)
        {
            if (kvp.Key.Item1 == dtoType)
            {
                mappings[kvp.Key.Item2] = kvp.Value;
            }
        }

        return mappings;
    }

    private static Dictionary<(Type, string), (string, string)> InitializeGlobalMappings()
    {
        var mappings = new Dictionary<(Type, string), (string, string)>();
        var assemblies = GetLoadedAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    ExtractMappingsFromType(type, mappings);
                }
            }
            catch
            {
            }
        }

        return mappings;
    }

    private static void ExtractMappingsFromType(
        Type type,
        Dictionary<(Type, string), (string, string)> mappings)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<ODataCollectionMappingAttribute>();

            if (attribute != null)
            {
                var key = (type, property.Name);
                var value = (attribute.EntityCollectionProperty, attribute.EntityItemProperty);
                mappings[key] = value;
            }
        }
    }

    private static IEnumerable<Assembly> GetLoadedAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Distinct();
    }
}
