using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Package;

public class FissionPackageRef
{
    [JsonPropertyName("packageref")]
    public FissionPackageReference Packageref { get; set; }
}
