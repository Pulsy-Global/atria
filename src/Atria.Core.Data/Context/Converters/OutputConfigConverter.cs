using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Outputs.Config;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection;
using System.Text.Json;

namespace Atria.Core.Data.Context.Converters;

public class OutputConfigConverter : ValueConverter<OutputConfigBase?, string?>
{
    private static readonly Dictionary<OutputType, Type> _typeMapping = new();

    static OutputConfigConverter()
    {
        InitializeMappings();
    }

    public OutputConfigConverter()
        : base(
            v => SerializeToJson(v),
            v => DeserializeFromJson(v))
    {
    }

    private static void InitializeMappings()
    {
        var assemblies = GetAssemblies();

        var getConfigClasses = (Type type) =>
        {
            return type.IsSubclassOf(typeof(OutputConfigBase)) && !type.IsAbstract;
        };

        var configTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => getConfigClasses(type));

        foreach (var configType in configTypes)
        {
            var instance = Activator.CreateInstance(configType) as OutputConfigBase;

            if (instance != null)
            {
                _typeMapping[instance.OutputType] = configType;
            }
        }
    }

    private static string? SerializeToJson(OutputConfigBase? outputConfig)
    {
        if (outputConfig == null)
        {
            return null;
        }

        var test = JsonSerializer.Serialize(outputConfig, outputConfig.GetType());

        return JsonSerializer.Serialize(outputConfig, outputConfig.GetType());
    }

    private static OutputConfigBase? DeserializeFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            var rootElement = document.RootElement;
            var property = rootElement.GetProperty("outputType");

            var outputType = property.ValueKind switch
            {
                JsonValueKind.Number => (OutputType)property.GetInt32(),
                JsonValueKind.String => Enum.Parse<OutputType>(property.GetString() !),
                _ => throw new InvalidOperationException("Invalid OutputType value")
            };

            if (_typeMapping.TryGetValue(outputType, out var configType))
            {
                return (OutputConfigBase?)JsonSerializer.Deserialize(json, configType);
            }

            throw new InvalidOperationException($"No type mapping found for OutputType: {outputType}");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize OutputConfig: {ex.Message}", ex);
        }
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
