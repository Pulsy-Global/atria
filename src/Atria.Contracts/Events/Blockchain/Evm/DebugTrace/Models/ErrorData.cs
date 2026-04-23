using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.DebugTrace.Models;

public class ErrorData
{
    [JsonPropertyName("code")]
    public long Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }
}
