using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.BlockWithTransactions.Models;

public record BlockWithTransactionData
{
    [JsonPropertyName("transactions")]
    public TransactionsInfoData[] TransactionsInfo { get; set; }

    [JsonPropertyName("number")]
    public string Number { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; }

    [JsonPropertyName("author")]
    public object Author { get; set; }

    [JsonPropertyName("sealFields")]
    public object SealFields { get; set; }

    [JsonPropertyName("parentHash")]
    public string ParentHash { get; set; }

    [JsonPropertyName("nonce")]
    public string Nonce { get; set; }

    [JsonPropertyName("sha3Uncles")]
    public string Sha3Uncles { get; set; }

    [JsonPropertyName("logsBloom")]
    public string LogsBloom { get; set; }

    [JsonPropertyName("transactionsRoot")]
    public string TransactionsRoot { get; set; }

    [JsonPropertyName("stateRoot")]
    public string StateRoot { get; set; }

    [JsonPropertyName("receiptsRoot")]
    public string ReceiptsRoot { get; set; }

    [JsonPropertyName("miner")]
    public string Miner { get; set; }

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; }

    [JsonPropertyName("totalDifficulty")]
    public object TotalDifficulty { get; set; }

    [JsonPropertyName("mixHash")]
    public string MixHash { get; set; }

    [JsonPropertyName("extraData")]
    public string ExtraData { get; set; }

    [JsonPropertyName("size")]
    public string Size { get; set; }

    [JsonPropertyName("gasLimit")]
    public string GasLimit { get; set; }

    [JsonPropertyName("gasUsed")]
    public string GasUsed { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; }

    [JsonPropertyName("uncles")]
    public object[] Uncles { get; set; }

    [JsonPropertyName("baseFeePerGas")]
    public string BaseFeePerGas { get; set; }

    [JsonPropertyName("withdrawalsRoot")]
    public string WithdrawalsRoot { get; set; }

    [JsonPropertyName("withdrawals")]
    public WithdrawalData[] Withdrawals { get; set; }
}
