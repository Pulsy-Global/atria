using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Function;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Ingress;
using System.Text.Json.Serialization;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.HttpTrigger;

public class FissionHttpTriggerSpec
{
    [JsonPropertyName("functionref")]
    public FissionFunctionReference FunctionRef { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = "";

    [JsonPropertyName("methods")]
    public string[] Methods { get; set; }

    [JsonPropertyName("relativeurl")]
    public string RelativeUrl { get; set; }

    [JsonPropertyName("createingress")]
    public bool CreateIngress { get; set; }

    [JsonPropertyName("host")]
    public string Host { get; set; } = "";

    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = "";

    [JsonPropertyName("ingressconfig")]
    public FissionIngressConfig IngressConfig { get; set; }

    [JsonPropertyName("tls")]
    public bool Tls { get; set; }
}
