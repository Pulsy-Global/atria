using Atria.Core.Data.Entities.Enums;
using System.Text.Json.Serialization;

namespace Atria.Orchestrator.Models.Dto.Feed;

public record SourceConfig
{
    [JsonPropertyName("networkId")]
    public string NetworkId { get; init; }

    [JsonPropertyName("dataType")]
    public AtriaDataType? DataType { get; init; }

    [JsonPropertyName("startBlock")]
    public string? StartBlock { get; init; }

    [JsonPropertyName("endBlock")]
    public string? EndBlock { get; init; }
}
