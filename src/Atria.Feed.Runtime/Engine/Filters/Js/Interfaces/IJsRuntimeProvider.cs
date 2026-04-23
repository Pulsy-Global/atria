using Microsoft.ClearScript.V8;

namespace Atria.Feed.Runtime.Engine.Filters.Js.Interfaces;

public interface IJsRuntimeProvider : IDisposable
{
    V8ScriptEngine CreateEngine(ISet<string> requiredModules);

    V8Script CompileUserCode(string code);

    string GetModuleNotFoundError(string moduleName);
}
