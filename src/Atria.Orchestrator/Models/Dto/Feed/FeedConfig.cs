using System.Text.Json.Serialization;

namespace Atria.Orchestrator.Models.Dto.Feed;

public record FeedConfig
{
    [JsonPropertyName("source")]
    public SourceConfig Source { get; init; }

    [JsonPropertyName("runtime")]
    public RuntimeConfig Runtime { get; init; }

    [JsonPropertyName("destination")]
    public OutputConfig? Output { get; init; }
}
