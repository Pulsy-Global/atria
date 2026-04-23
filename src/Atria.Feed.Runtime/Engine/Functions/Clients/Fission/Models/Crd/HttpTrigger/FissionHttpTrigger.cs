using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Common;
using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.HttpTrigger;

public class FissionHttpTrigger
{
    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; } = "fission.io/v1";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "HTTPTrigger";

    [JsonPropertyName("metadata")]
    public FissionMetadata Metadata { get; set; }

    [JsonPropertyName("spec")]
    public FissionHttpTriggerSpec Spec { get; set; }
}
