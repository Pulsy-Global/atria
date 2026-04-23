using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Package;

public class FissionEnvironmentReference
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; }
}
