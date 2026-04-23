using Atria.Feed.Runtime.Engine.Filters.Js.Interfaces;
using Atria.Feed.Runtime.Engine.Filters.Js.Models;
using Atria.Feed.Runtime.Engine.Filters.Js.Options;
using FuzzySharp;
using Microsoft.ClearScript;
using Microsoft.Extensions.Options;
using System.Text;

namespace Atria.Feed.Runtime.Engine.Filters.Js;

public class JsModuleRegistry : IJsModuleRegistry
{
    private static readonly string ScriptsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Scripts",
        "Filters",
        "Js");

    private readonly Dictionary<string, JsModuleConfig> _modules;
    private readonly Lazy<string> _baseCode;

    public JsModuleRegistry(IOptions<JsRuntimeOptions> options)
    {
        _modules = options.Value.Modules
            .ToDictionary(m => m.Name, m => m);

        _baseCode = new Lazy<string>(BuildBaseCode);
    }

    public IReadOnlyList<string> AvailableModules => _modules.Keys.ToList();

    public string GetBaseCode() => _baseCode.Value;

    public IReadOnlyList<string> GetDependencies(string moduleName)
    {
        return _modules.TryGetValue(moduleName, out var config) ? config.Dependencies : [];
    }

    public string GetModuleNotFoundError(string requestedModule)
    {
        var result = Process.ExtractOne(requestedModule, AvailableModules);
        var availableList = string.Join(", ", AvailableModules);

        if (result != null && result.Score >= 70)
        {
            return $"Module '{requestedModule}' not found. Did you mean '{result.Value}'? Available modules: {availableList}";
        }

        return $"Module '{requestedModule}' not found. Available modules: {availableList}";
    }

    public string? GetModuleCode(string moduleName)
    {
        if (!_modules.TryGetValue(moduleName, out var config))
        {
            return null;
        }

        var moduleCode = LoadScript(config.FileName);

        return $$"""
            (function() {
                var module = { exports: {} };
                var exports = module.exports;

                {{moduleCode}}

                globalThis.__modules['{{moduleName}}'] = module.exports;
            })();
            """;
    }

    private static string BuildBaseCode()
    {
        var sb = new StringBuilder();

        sb.AppendLine(LoadScript("wrapper.js"));

        sb.AppendLine(GetRequireImplementation());

        return sb.ToString();
    }

    private static string GetRequireImplementation()
    {
        return """
            globalThis.__modules = {};
            globalThis.__moduleCache = {};

            globalThis.require = function(name) {
                if (globalThis.__moduleCache[name]) {
                    return globalThis.__moduleCache[name];
                }
                if (globalThis.__modules[name]) {
                    globalThis.__moduleCache[name] = globalThis.__modules[name];
                    return globalThis.__moduleCache[name];
                }
                throw new Error("__MISSING_MODULE__:" + name);
            };
            """;
    }

    private static string LoadScript(string fileName)
    {
        var filePath = Path.Combine(ScriptsPath, fileName);

        if (!File.Exists(filePath))
        {
            throw new ScriptEngineException($"JavaScript file not found: {filePath}");
        }

        return File.ReadAllText(filePath);
    }
}
