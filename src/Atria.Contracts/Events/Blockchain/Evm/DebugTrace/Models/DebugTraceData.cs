using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.DebugTrace.Models;

public class DebugTraceData
{
    [JsonPropertyName("txHash")]
    public string TxHash { get; set; }

    [JsonPropertyName("result")]
    public ResultData Result { get; set; }
}
