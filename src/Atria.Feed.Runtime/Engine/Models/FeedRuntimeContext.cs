using Atria.Feed.Runtime.Engine.Filters.Interfaces;
using Atria.Feed.Runtime.Engine.Functions.Interfaces;

namespace Atria.Feed.Runtime.Engine.Models;

public class FeedRuntimeContext
{
    public FeedRuntime FeedRuntime { get; set; }
    public IFilterContext? FilterContext { get; set; }
    public IFunctionContext? FunctionContext { get; set; }
}
