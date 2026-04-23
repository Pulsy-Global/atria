using Newtonsoft.Json;

namespace Atria.Feed.Ingestor.ChainClients.Models;

public class RpcRequest
{
    [JsonProperty("id")]
    public int Id { get; set; } = 1;

    [JsonProperty("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("params")]
    public object[] Params { get; set; }

    public RpcRequest(string method, params object[] parameters)
    {
        Method = method;
        Params = parameters;
    }
}
