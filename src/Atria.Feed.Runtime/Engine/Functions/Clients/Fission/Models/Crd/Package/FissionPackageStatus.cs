using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Package;

public class FissionPackageStatus
{
    [JsonPropertyName("buildstatus")]
    public string BuildStatus { get; set; } = "succeeded";

    [JsonPropertyName("lastUpdateTimestamp")]
    public string LastUpdateTimestamp { get; set; }
}

