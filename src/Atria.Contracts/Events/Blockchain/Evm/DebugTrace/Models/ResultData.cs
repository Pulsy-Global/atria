using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.DebugTrace.Models;

public class ResultData
{
    [JsonPropertyName("from")]
    public string From { get; set; }

    [JsonPropertyName("gas")]
    public string Gas { get; set; }

    [JsonPropertyName("gasUsed")]
    public string GasUsed { get; set; }

    [JsonPropertyName("input")]
    public string Input { get; set; }

    [JsonPropertyName("to")]
    public string To { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("calls")]
    public ResultCallData[] Calls { get; set; }

    [JsonPropertyName("output")]
    public string Output { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; }
}
