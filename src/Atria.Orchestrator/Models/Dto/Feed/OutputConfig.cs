using Atria.Core.Data.Entities.Enums;
using System.Text.Json.Serialization;

namespace Atria.Orchestrator.Models.Dto.Feed;

public record OutputConfig
{
    [JsonPropertyName("outputs")]
    public List<string> Outputs { get; init; }

    [JsonPropertyName("errorHandling")]
    public ErrorHandlingStrategy ErrorHandling { get; init; }
}
