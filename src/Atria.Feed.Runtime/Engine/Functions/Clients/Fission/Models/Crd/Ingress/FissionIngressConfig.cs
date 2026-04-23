using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Ingress;

public class FissionIngressConfig
{
    [JsonPropertyName("host")]
    public string Host { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("tls")]
    public string Tls { get; set; }

    [JsonPropertyName("annotations")]
    public object? Annotations { get; set; }
}
