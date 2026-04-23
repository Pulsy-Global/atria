using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.BlockWithTransactions.Models;

public record WithdrawalData
{
    [JsonPropertyName("index")]
    public string Index { get; set; }

    [JsonPropertyName("validatorIndex")]
    public string ValidatorIndex { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("amount")]
    public string Amount { get; set; }
}
