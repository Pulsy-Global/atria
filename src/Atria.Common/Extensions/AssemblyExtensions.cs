using System.Reflection;

namespace Atria.Common.Extensions;

public static class AssemblyExtensions
{
    public static string? GetAssemblyVersion(this Assembly assembly) =>
        assembly?.GetName()?.Version?.ToString();

    public static string? GetInformationalVersion(this Assembly assembly) =>
        assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    public static IEnumerable<Type> GetTypesImplementedInterface<T>(this Assembly assembly)
    {
        var targetType = typeof(T);

        foreach (var typeInfo in assembly.DefinedTypes)
        {
            if (targetType.IsAssignableFrom(typeInfo) && !typeInfo.IsInterface && !typeInfo.IsAbstract)
            {
                yield return typeInfo.AsType();
            }
        }

        var referencedAssemblies = assembly.GetReferencedAssemblies();

        foreach (var assemblyName in referencedAssemblies)
        {
            Assembly currentAssembly;
            try
            {
                currentAssembly = Assembly.Load(assemblyName);
            }
            catch
            {
                continue;
            }

            foreach (var typeInfo in currentAssembly.DefinedTypes)
            {
                if (targetType.IsAssignableFrom(typeInfo) && !typeInfo.IsInterface && !typeInfo.IsAbstract)
                {
                    yield return typeInfo.AsType();
                }
            }
        }
    }
}
