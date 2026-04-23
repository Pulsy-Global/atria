namespace Atria.Feed.Runtime.Engine.Filters.Js.Models;

public class JsModuleConfig
{
    public string Name { get; set; }

    public string FileName { get; set; }

    public List<string> Dependencies { get; set; } = [];
}
