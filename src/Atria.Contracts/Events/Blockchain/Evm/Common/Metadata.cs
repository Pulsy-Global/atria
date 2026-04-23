using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.Common;

public class Metadata
{
    [JsonPropertyName("networkId")]
    public string NetworkId { get; set; }

    [JsonPropertyName("blockNumber")]
    public string BlockNumber { get; set; }

    [JsonPropertyName("isReorg")]
    public bool IsReorg { get; set; } = false;
}
