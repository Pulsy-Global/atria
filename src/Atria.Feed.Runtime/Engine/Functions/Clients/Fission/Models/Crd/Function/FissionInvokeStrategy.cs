using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Function;

public class FissionInvokeStrategy
{
    [JsonPropertyName("StrategyType")]
    public string StrategyType { get; set; } = "execution";

    [JsonPropertyName("ExecutionStrategy")]
    public FissionExecutionStrategy ExecutionStrategy { get; set; } = new();
}
