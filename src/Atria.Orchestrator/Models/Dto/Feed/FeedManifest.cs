using System.Text.Json.Serialization;

namespace Atria.Orchestrator.Models.Dto.Feed;

public record FeedManifest
{
    [JsonPropertyName("name")]
    public string Name { get; init; }

    [JsonPropertyName("version")]
    public string Version { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("config")]
    public FeedConfig Config { get; init; }
}
