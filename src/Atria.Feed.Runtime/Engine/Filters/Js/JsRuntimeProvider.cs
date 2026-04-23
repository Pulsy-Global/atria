using Atria.Feed.Runtime.Engine.Filters.Js.Interfaces;
using Atria.Feed.Runtime.Engine.Filters.Js.Options;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atria.Feed.Runtime.Engine.Filters.Js;

public class JsRuntimeProvider : IJsRuntimeProvider
{
    private const V8ScriptEngineFlags EngineFlags =
        V8ScriptEngineFlags.HideHostExceptions |
        V8ScriptEngineFlags.EnableStringifyEnhancements |
        V8ScriptEngineFlags.EnableTaskPromiseConversion;

    private const nuint MaxSafeStackBytes = 512 * 1024;

    private readonly V8Runtime _runtime;
    private readonly V8Script _baseScript;
    private readonly Dictionary<string, V8Script> _moduleScripts = new();
    private readonly IJsModuleRegistry _moduleRegistry;
    private readonly JsRuntimeOptions _options;
    private readonly ILogger<JsRuntimeProvider> _logger;

    private bool _disposed;

    public JsRuntimeProvider(
        IJsModuleRegistry moduleRegistry,
        IOptions<JsRuntimeOptions> options,
        ILogger<JsRuntimeProvider> logger)
    {
        _moduleRegistry = moduleRegistry;
        _options = options.Value;
        _logger = logger;

        var heapLimitMB = _options.MaxOldSpaceSizeMB;
        var detectionSource = "Config";

        if (heapLimitMB <= 0)
        {
            var availableBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            heapLimitMB = Math.Clamp((int)(availableBytes * 0.8 / (1024 * 1024)), 512, 4096);
            detectionSource = "Auto-detected";
        }

        var constraints = new V8RuntimeConstraints
        {
            MaxOldSpaceSize = heapLimitMB * 1024 * 1024,
        };

        _runtime = new V8Runtime(constraints);
        _runtime.HeapSizeSampleInterval = TimeSpan.FromMilliseconds(_options.GcIntervalMs);
        _runtime.MaxHeapSize = (UIntPtr)((ulong)heapLimitMB * 1024 * 1024);

        _logger.LogInformation(
            "Created V8 runtime with heap limit {HeapLimitMB}MB (Source: {Source})",
            heapLimitMB,
            detectionSource);

        _baseScript = _runtime.Compile(moduleRegistry.GetBaseCode());

        foreach (var moduleName in moduleRegistry.AvailableModules)
        {
            var moduleCode = moduleRegistry.GetModuleCode(moduleName);
            if (moduleCode != null)
            {
                _moduleScripts[moduleName] = _runtime.Compile(moduleCode);
            }
        }

        _logger.LogInformation(
            "Pre-compiled {Count} modules: {Modules}",
            _moduleScripts.Count,
            string.Join(", ", _moduleScripts.Keys));
    }

    public V8ScriptEngine CreateEngine(ISet<string> requiredModules)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            return CreateEngineInternal(requiredModules);
        }
        catch (ScriptEngineException)
        {
            _runtime.CollectGarbage(true);
            return CreateEngineInternal(requiredModules);
        }
    }

    public V8Script CompileUserCode(string code)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _runtime.Compile(code);
    }

    public string GetModuleNotFoundError(string moduleName)
        => _moduleRegistry.GetModuleNotFoundError(moduleName);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _runtime.Dispose();

        _logger.LogInformation("V8 runtime disposed");
    }

    private V8ScriptEngine CreateEngineInternal(ISet<string> requiredModules)
    {
        var resolvedModules = ResolveDependencies(requiredModules);
        var engine = _runtime.CreateScriptEngine(EngineFlags);

        engine.MaxRuntimeHeapSize = (nuint)_options.MaxHeapSizeMB * 1024 * 1024;

        var configuredStack = (nuint)_options.MaxStackSizeKB * 1024;
        engine.MaxRuntimeStackUsage = Math.Min(configuredStack, MaxSafeStackBytes);

        engine.Execute(_baseScript);

        foreach (var moduleName in resolvedModules)
        {
            if (_moduleScripts.TryGetValue(moduleName, out var moduleScript))
            {
                engine.Execute(moduleScript);
            }
        }

        engine.Script.__cleanupDangerousFunctions();

        return engine;
    }

    private HashSet<string> ResolveDependencies(ISet<string> modules)
    {
        var result = new HashSet<string>(modules);
        var stack = new Stack<string>(modules);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            var dependencies = _moduleRegistry.GetDependencies(current);

            foreach (var dependency in dependencies)
            {
                if (result.Add(dependency))
                {
                    stack.Push(dependency);
                }
            }
        }

        return result;
    }
}
