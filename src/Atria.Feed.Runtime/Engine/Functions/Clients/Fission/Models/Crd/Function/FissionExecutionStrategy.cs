using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Function;

public class FissionExecutionStrategy
{
    [JsonPropertyName("ExecutorType")]
    public string ExecutorType { get; set; } = "poolmgr";

    [JsonPropertyName("MinScale")]
    public int MinScale { get; set; } = 1;

    [JsonPropertyName("MaxScale")]
    public int MaxScale { get; set; } = 1;

    [JsonPropertyName("SpecializationTimeout")]
    public int SpecializationTimeout { get; set; } = 120;

    [JsonPropertyName("TargetCPUPercent")]
    public int TargetCPUPercent { get; set; } = 0;
}
