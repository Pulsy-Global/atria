using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.DebugTrace.Models.Response;

public class ResultElementResponse
{
    [JsonPropertyName("txHash")]
    public string TxHash { get; set; }

    [JsonPropertyName("result")]
    public ResultResponse Result { get; set; }
}
