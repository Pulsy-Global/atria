using Atria.Core.Data.Entities.Feeds;
using Atria.Orchestrator.Models.Business;
using Atria.Orchestrator.Models.Dto.Feed;

namespace Atria.Orchestrator.Services.Interfaces;

public interface IFeedProvisioningManager
{
    Task<Feed> CreateFeedFromManifestAsync(FeedManifest manifest, string hash, CancellationToken ct = default);

    Task<Feed> UpdateFeedFromManifestAsync(FeedManifest manifest, string hash, CancellationToken ct = default);

    Task DeleteFeedAsync(Guid feedId, CancellationToken ct = default);

    Task<ScanResult<FeedDeployment>> CompareFeedsWithManifestsAsync(
        List<DirectoryScanResult<FeedManifest>> manifests,
        CancellationToken ct = default);

    bool ValidateFeedManifest(FeedManifest feedManifest);
}
