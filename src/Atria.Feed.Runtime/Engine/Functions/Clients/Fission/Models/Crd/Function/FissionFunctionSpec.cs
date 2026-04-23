using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Environment;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Package;
using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Function;

public class FissionFunctionSpec
{
    [JsonPropertyName("environment")]
    public FissionEnvironmentRef Environment { get; set; }

    [JsonPropertyName("package")]
    public FissionPackageRef Package { get; set; }

    [JsonPropertyName("entrypoint")]
    public string Entrypoint { get; set; }

    [JsonPropertyName("InvokeStrategy")]
    public FissionInvokeStrategy InvokeStrategy { get; set; }

    [JsonPropertyName("concurrency")]
    public int Concurrency { get; set; } = 500;

    [JsonPropertyName("functionTimeout")]
    public int FunctionTimeout { get; set; } = 60;

    [JsonPropertyName("idletimeout")]
    public int IdleTimeout { get; set; } = 120;

    [JsonPropertyName("requestsPerPod")]
    public int RequestsPerPod { get; set; } = 1;

    [JsonPropertyName("resources")]
    public object Resources { get; set; } = new { };
}
