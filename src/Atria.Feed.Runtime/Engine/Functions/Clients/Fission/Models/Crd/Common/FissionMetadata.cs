using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Common;

public class FissionMetadata
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; }

    [JsonPropertyName("resourceVersion")]
    public string? ResourceVersion { get; set; }
}
