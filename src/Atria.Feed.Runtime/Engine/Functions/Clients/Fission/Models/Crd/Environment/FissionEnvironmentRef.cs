using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Environment;

public class FissionEnvironmentRef
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = "default";
}
