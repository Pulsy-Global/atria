using Atria.Orchestrator.Models.Dto.Feed;

namespace Atria.Orchestrator.Models.Business;

public record FeedDeployment
{
    public FeedManifest Manifest { get; init; } = new();
    public string Hash { get; init; } = string.Empty;
}
