using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.BlockWithLogs.Models;

public record EvmLogData
{
    [JsonPropertyName("removed")]
    public bool Removed { get; set; }

    [JsonPropertyName("type")]
    public object Type { get; set; }

    [JsonPropertyName("logIndex")]
    public string LogIndex { get; set; }

    [JsonPropertyName("transactionHash")]
    public string TransactionHash { get; set; }

    [JsonPropertyName("transactionIndex")]
    public string TransactionIndex { get; set; }

    [JsonPropertyName("blockHash")]
    public string BlockHash { get; set; }

    [JsonPropertyName("blockNumber")]
    public string BlockNumber { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("data")]
    public string Data { get; set; }

    [JsonPropertyName("topics")]
    public string[] Topics { get; set; }
}
