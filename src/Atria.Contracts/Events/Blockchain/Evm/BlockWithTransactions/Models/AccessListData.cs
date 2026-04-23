using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.BlockWithTransactions.Models;

public record AccessListData
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("storageKeys")]
    public string[] StorageKeys { get; set; }
}
