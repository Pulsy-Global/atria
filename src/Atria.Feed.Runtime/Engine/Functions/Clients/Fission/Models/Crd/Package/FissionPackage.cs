using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Common;
using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Package;

public class FissionPackage
{
    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; } = "fission.io/v1";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "Package";

    [JsonPropertyName("metadata")]
    public FissionMetadata Metadata { get; set; }

    [JsonPropertyName("spec")]
    public FissionPackageSpec Spec { get; set; }

    [JsonPropertyName("status")]
    public FissionPackageStatus Status { get; set; }
}

