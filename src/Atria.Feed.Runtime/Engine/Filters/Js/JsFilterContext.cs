using Atria.Feed.Runtime.Engine.Exceptions;
using Atria.Feed.Runtime.Engine.Filters.Interfaces;
using Atria.Feed.Runtime.Engine.Filters.Js.Interfaces;
using Atria.Feed.Runtime.Engine.Filters.Js.Options;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Atria.Feed.Runtime.Engine.Filters.Js;

public partial class JsFilterContext : IFilterContext
{
    private readonly V8ScriptEngine _engine;
    private readonly IJsRuntimeProvider _runtimeProvider;
    private readonly Lock _syncRoot = new();
    private readonly int _gcEvery;
    private readonly int _maxOutputBytes;

    private int _executionCount;

    public JsFilterContext(
        IJsRuntimeProvider runtimeProvider,
        IOptions<JsRuntimeOptions> options,
        string code,
        IKvHostBridge? kvBridge = null)
    {
        _runtimeProvider = runtimeProvider;
        _gcEvery = options.Value.GcEveryNExecutions;
        _maxOutputBytes = options.Value.MaxOutputSizeKB * 1024;

        var requiredModules = ParseRequires(code);

        _engine = runtimeProvider.CreateEngine(requiredModules);

        if (kvBridge != null)
        {
            _engine.AddHostObject("__kvHost", kvBridge);
        }

        try
        {
            var userScript = runtimeProvider.CompileUserCode(code);
            _engine.Execute(userScript);
        }
        catch (ScriptEngineException e)
        {
            _engine.Dispose();
            throw HandleException(e, code);
        }
    }

    public async Task<object?> ExecuteAsync(string fn, object? input, CancellationToken ct = default)
    {
        if (_engine.Script[fn] is not ScriptObject call)
        {
            throw new ScriptEngineException($"Function: '{fn}' not found");
        }

        try
        {
            var jsonInput = input switch
            {
                null => "null",
                JsonElement je => je.GetRawText(),
                _ => JsonSerializer.Serialize(input),
            };

            await using var reg = ct.Register(() => _engine.Interrupt());

            var result = await Task.Run(
                () => _engine.Script.__executeAsync(call, jsonInput),
                ct).WaitAsync(ct);

            if (result is Task task)
            {
                await task.WaitAsync(ct);
                result = task is Task<object> typed ? typed.Result : null;
            }

            TrackExecution();

            if (result is null or Undefined || (result is string s && s == "null"))
            {
                return null;
            }

            var json = (string)result;
            var byteCount = System.Text.Encoding.UTF8.GetByteCount(json);
            if (byteCount > _maxOutputBytes)
            {
                throw new FeedEngineException(
                    $"Filter output too large: {byteCount / 1024}KB exceeds limit of {_maxOutputBytes / 1024}KB",
                    string.Empty);
            }

            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (ScriptEngineException e) when (ct.IsCancellationRequested)
        {
            _engine.CollectGarbage(true);
            throw new OperationCanceledException("Script execution was interrupted", e, ct);
        }
        catch (ScriptEngineException e)
        {
            _engine.CollectGarbage(true);
            throw HandleException(e, _engine.Script.ToString());
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_syncRoot)
        {
            _engine.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    internal static HashSet<string> ParseRequires(string code)
    {
        var modules = new HashSet<string>();
        var matches = RequireRegex().Matches(code);

        foreach (Match match in matches)
        {
            modules.Add(match.Groups[1].Value);
        }

        return modules;
    }

    [GeneratedRegex(@"require\s*\(\s*['""]([^'""]+)['""]\s*\)")]
    private static partial Regex RequireRegex();

    private void TrackExecution()
    {
        lock (_syncRoot)
        {
            _executionCount++;
            if (_executionCount % _gcEvery == 0)
            {
                _engine.CollectGarbage(false);
            }
        }
    }

    private FeedEngineException HandleException(ScriptEngineException e, string code)
    {
        var message = e.Message;
        var parts = message.Split("__MISSING_MODULE__:");

        if (parts.Length > 1)
        {
            var moduleName = parts[1].Split('\n')[0].Trim();
            message = _runtimeProvider.GetModuleNotFoundError(moduleName);
        }

        return new FeedEngineException(message, code, e);
    }
}
