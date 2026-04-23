using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.BlockWithTransactions.Models;

public record TransactionsInfoData
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; }

    [JsonPropertyName("transactionIndex")]
    public string TransactionIndex { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("blockHash")]
    public string BlockHash { get; set; }

    [JsonPropertyName("blockNumber")]
    public string BlockNumber { get; set; }

    [JsonPropertyName("from")]
    public string From { get; set; }

    [JsonPropertyName("to")]
    public string To { get; set; }

    [JsonPropertyName("gas")]
    public string Gas { get; set; }

    [JsonPropertyName("gasPrice")]
    public string GasPrice { get; set; }

    [JsonPropertyName("maxFeePerGas")]
    public string MaxFeePerGas { get; set; }

    [JsonPropertyName("maxPriorityFeePerGas")]
    public string MaxPriorityFeePerGas { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("input")]
    public string Input { get; set; }

    [JsonPropertyName("nonce")]
    public string Nonce { get; set; }

    [JsonPropertyName("r")]
    public string R { get; set; }

    [JsonPropertyName("s")]
    public string S { get; set; }

    [JsonPropertyName("v")]
    public string V { get; set; }

    [JsonPropertyName("accessList")]
    public AccessListData[] AccessList { get; set; }

    [JsonPropertyName("authorizationList")]
    public object AuthorizationList { get; set; }
}
