using System.Text;

namespace Atria.Common.Web.Swagger.Helpers;

public static class SchemaIdSelector
{
    public static Func<Type, string?> CustomSchemaIdFactory(List<Type> customGenericTypes)
    {
        return (type) =>
        {
            var systemTypeName = type.FullName;

            if (systemTypeName?.EndsWith("Dto") ?? false)
            {
                systemTypeName = systemTypeName.Substring(0, systemTypeName.Length - 3);
            }

            if (type.IsGenericType && customGenericTypes.Any(x => type.GetGenericTypeDefinition() == x))
            {
                var typeParameters = type.GetGenericArguments();

                var baseTypeNameSoFar = new StringBuilder(
                    systemTypeName?.Substring(0, systemTypeName.IndexOf('`')));

                foreach (var typeParameter in typeParameters)
                {
                    baseTypeNameSoFar.Append($"_{typeParameter.Name}");
                }

                return baseTypeNameSoFar.ToString();
            }

            return systemTypeName;
        };
    }
}
