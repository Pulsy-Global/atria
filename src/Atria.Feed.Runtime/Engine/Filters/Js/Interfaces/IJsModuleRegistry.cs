namespace Atria.Feed.Runtime.Engine.Filters.Js.Interfaces;

public interface IJsModuleRegistry
{
    string GetBaseCode();

    string? GetModuleCode(string moduleName);

    IReadOnlyList<string> AvailableModules { get; }

    IReadOnlyList<string> GetDependencies(string moduleName);

    string GetModuleNotFoundError(string requestedModule);
}
