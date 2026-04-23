using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.DebugTrace.Models.Response;

public class DebugTraceResponse
{
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("result")]
    public ResultElementResponse[] Result { get; set; }

    [JsonPropertyName("error")]
    public ErrorResponse Error { get; set; }
}
