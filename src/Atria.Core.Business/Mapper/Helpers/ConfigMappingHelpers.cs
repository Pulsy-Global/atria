using Atria.Core.Business.Mapper.Attributes;
using Atria.Core.Data.Entities.Enums;
using Mapster;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Reflection;

namespace Atria.Core.Business.Mapper.Helpers;

public static class ConfigMappingHelpers
{
    private static readonly Dictionary<OutputType, (Type EntityType, Type DtoType)> _mappings = new();

    static ConfigMappingHelpers()
    {
        var assemblies = GetAssemblies();

        var getAttribute = (Type type) =>
        {
            return type.GetCustomAttribute<ConfigMappingAttribute>();
        };

        var typesWithAttribute = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => getAttribute(type) != null);

        foreach (var type in typesWithAttribute)
        {
            var attribute = getAttribute(type);

            if (attribute != null)
            {
                _mappings[attribute.OutputType] = (
                    attribute.EntityType,
                    attribute.DtoType);
            }
        }
    }

    public static void RegisterMappings(TypeAdapterConfig config)
    {
        foreach (var (outputType, (entityType, dtoType)) in _mappings)
        {
            TypeAdapterConfig.GlobalSettings.NewConfig(entityType, dtoType);
            TypeAdapterConfig.GlobalSettings.NewConfig(dtoType, entityType);
        }
    }

    public static (Type EntityType, Type DtoType) GetMapping(OutputType outputType)
    {
        return !_mappings.TryGetValue(outputType, out var mapping)
            ? throw new InvalidOperationException(
                $"No mapping found for OutputType: {outputType}")
            : (mapping.EntityType, mapping.DtoType);
    }

    private static IEnumerable<Assembly> GetAssemblies()
    {
        var currentAssembly = Assembly.GetExecutingAssembly();

        var referencedAssemblies = currentAssembly
            .GetReferencedAssemblies()
            .Select(Assembly.Load);

        return new[] { currentAssembly }.Concat(referencedAssemblies);
    }
}
