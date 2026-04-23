using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Package;

public class FissionPackageSpec
{
    [JsonPropertyName("deployment")]
    public FissionPackageDeployment Deployment { get; set; }

    [JsonPropertyName("environment")]
    public FissionEnvironmentReference Environment { get; set; }
}
