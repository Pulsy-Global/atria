using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Function;

public class FissionFunctionReference
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("functionweights")]
    public object? FunctionWeights { get; set; }
}
