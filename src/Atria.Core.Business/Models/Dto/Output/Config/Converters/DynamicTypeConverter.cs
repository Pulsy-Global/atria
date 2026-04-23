using Atria.Core.Business.Mapper.Helpers;
using Atria.Core.Business.Models.Dto.Output.Config.Attributes;
using Atria.Core.Data.Entities.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Atria.Core.Business.Models.Dto.Output.Config.Converters;

public class DynamicTypeConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.GetProperties().Any(p => p.GetCustomAttribute<DynamicTypeAttribute>() != null);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        var instance = Activator.CreateInstance(objectType);

        var properties = objectType.GetProperties();

        foreach (var property in properties)
        {
            var dynamicTypeAttr = property.GetCustomAttribute<DynamicTypeAttribute>();
            var propertyName = GetPropertyNameFromInfo(property);

            if (dynamicTypeAttr != null)
            {
                if (!jObject.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out var configToken))
                {
                    return instance;
                }

                var typeProperty = FindTypeProperty(properties, dynamicTypeAttr.TypePropertyName);

                if (typeProperty == null)
                {
                    return instance;
                }

                var typeName = GetPropertyNameFromInfo(typeProperty);

                if (!jObject.TryGetValue(typeName, StringComparison.OrdinalIgnoreCase, out var typeToken))
                {
                    return instance;
                }

                if (Enum.TryParse<OutputType>(typeToken.ToString(), out var outputType))
                {
                    var (entityType, dtoType) = ConfigMappingHelpers.GetMapping(outputType);

                    var configValue = configToken.ToObject(dtoType, serializer);

                    property.SetValue(instance, configValue);
                }
            }
            else if (jObject.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out var token))
            {
                var value = token.ToObject(property.PropertyType, serializer);

                property.SetValue(instance, value);
            }
        }

        return instance;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var jObject = new JObject();

        var properties = value
            .GetType()
            .GetProperties();

        foreach (var property in properties)
        {
            var propertyValue = property.GetValue(value);

            if (propertyValue != null)
            {
                var propertyName = GetPropertyNameFromInfo(property);

                jObject[propertyName] = JToken.FromObject(
                    propertyValue, serializer);
            }
        }

        jObject.WriteTo(writer);
    }

    private PropertyInfo? FindTypeProperty(PropertyInfo[] properties, string typePropertyName)
    {
        return properties.FirstOrDefault(p => GetPropertyNameFromInfo(p)
            .Equals(typePropertyName, StringComparison.OrdinalIgnoreCase));
    }

    private string GetPropertyNameFromInfo(PropertyInfo property)
    {
        var jsonPropertyAttr = property.GetCustomAttribute<JsonPropertyAttribute>();

        return jsonPropertyAttr?.PropertyName ?? property.Name;
    }
}
