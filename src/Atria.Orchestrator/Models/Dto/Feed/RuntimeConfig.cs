using Atria.Core.Data.Entities.Enums;
using System.Text.Json.Serialization;

namespace Atria.Orchestrator.Models.Dto.Feed;

public record RuntimeConfig
{
    [JsonPropertyName("outputs")]
    public string[] Outputs { get; init; } = [];

    [JsonPropertyName("filter")]
    public PathConfig Filter { get; init; }

    [JsonPropertyName("function")]
    public PathConfig Function { get; init; }

    [JsonPropertyName("errorHandling")]
    public ErrorHandlingStrategy ErrorHandling { get; init; }
}
