using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Package;

public class FissionPackageDeployment
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "literal";

    [JsonPropertyName("literal")]
    public string Literal { get; set; }
}
