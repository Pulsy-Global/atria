using Atria.Feed.Runtime.Engine.Functions.Options;

namespace Atria.Feed.Runtime.Configuration.Options;

public class RuntimeOptions
{
    public int ExecutionTimeoutSec { get; set; } = 5;

    public FissionOptions Fission { get; set; }
}
