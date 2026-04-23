using Atria.Feed.Runtime.Engine.Filters.Js.Models;

namespace Atria.Feed.Runtime.Engine.Filters.Js.Options;

public class JsRuntimeOptions
{
    public int MaxOldSpaceSizeMB { get; set; } = 256;

    public int MaxHeapSizeMB { get; set; } = 48;

    public int MaxStackSizeKB { get; set; } = 512;

    public int MaxOutputSizeKB { get; set; } = 8192;

    public int GcIntervalMs { get; set; } = 100;

    public int GcEveryNExecutions { get; set; } = 10;

    public List<JsModuleConfig> Modules { get; set; } = [];
}
