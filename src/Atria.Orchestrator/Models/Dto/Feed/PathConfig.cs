using System.Text.Json.Serialization;

namespace Atria.Orchestrator.Models.Dto.Feed;

public record PathConfig
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;
}
