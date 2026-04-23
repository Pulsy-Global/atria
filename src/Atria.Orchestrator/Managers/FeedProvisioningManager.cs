using Atria.Business.Services.DataServices.Interfaces;
using Atria.Core.Data.Entities.Feeds;
using Atria.Orchestrator.Models.Business;
using Atria.Orchestrator.Models.Dto.Feed;
using Atria.Orchestrator.Services.Interfaces;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Atria.Orchestrator.Managers;

public class FeedProvisioningManager : BaseManager, IFeedProvisioningManager
{
    private readonly IMapper _mapper;
    private readonly ILogger<FeedProvisioningManager> _logger;
    private readonly IFeedDataService _feedDataService;
    private readonly IOutputDataService _outputDataService;

    public FeedProvisioningManager(
        IMapper mapper,
        ILogger<FeedProvisioningManager> logger,
        IFeedDataService feedDataService,
        IOutputDataService outputDataService)
        : base(logger, mapper)
    {
        _mapper = mapper;
        _logger = logger;
        _feedDataService = feedDataService;
        _outputDataService = outputDataService;
    }

    public async Task<Feed> CreateFeedFromManifestAsync(FeedManifest manifest, string hash, CancellationToken ct = default)
    {
        var feed = _mapper.Map<Feed>(manifest);

        feed.IsLocal = true;
        feed.Hash = hash;

        var manifestOutputs = manifest.Config.Output?.Outputs ?? new List<string>();

        var outputs = await _outputDataService
            .GetOutputsAsync(x => manifestOutputs.Contains(x.Name), ct);

        var outputIds = outputs.Select(x => x.Id).ToList();

        await _feedDataService.CreateFeedAsync(feed, ct, outputIds);

        _logger.LogInformation("Feed created: {Name} v{Version}", feed.Name, feed.Version);

        return feed;
    }

    public async Task<Feed> UpdateFeedFromManifestAsync(FeedManifest manifest, string hash, CancellationToken ct = default)
    {
        var feeds = await _feedDataService.GetFeedsAsync(
            x => x.Name == manifest.Name, ct, x => x.FeedOutputs);

        var feed = feeds.FirstOrDefault();
        if (feed == null)
        {
            throw new InvalidOperationException($"Feed with name {manifest.Name} not found");
        }

        _mapper.Map(manifest, feed);

        feed.IsLocal = true;
        feed.Hash = hash;

        var manifestOutputs = manifest.Config.Output?.Outputs ?? new List<string>();

        var outputs = await _outputDataService
            .GetOutputsAsync(x => manifestOutputs.Contains(x.Name), ct);

        var outputIds = outputs.Select(x => x.Id).ToList();

        await _feedDataService.UpdateFeedAsync(feed, ct, outputIds);

        _logger.LogInformation("Feed updated: {Name} v{Version}", feed.Name, feed.Version);

        return feed;
    }

    public async Task DeleteFeedAsync(Guid feedId, CancellationToken ct = default)
    {
        await _feedDataService.DeleteFeedAsync(feedId, ct);

        _logger.LogInformation("Feed deleted: {FeedId}", feedId);
    }

    public async Task<ScanResult<FeedDeployment>> CompareFeedsWithManifestsAsync(
        List<DirectoryScanResult<FeedManifest>> manifests,
        CancellationToken ct = default)
    {
        var existingFeeds = await _feedDataService.GetFeedsAsync(x => x.IsLocal, ct);

        var result = new ScanResult<FeedDeployment>();

        foreach (var manifestResult in manifests)
        {
            var manifest = manifestResult.Item;

            var existingFeed = existingFeeds.FirstOrDefault(f => f.Name == manifest.Name);

            var feedDeployment = new FeedDeployment
            {
                Manifest = manifest,
                Hash = manifestResult.FileHash,
            };

            if (existingFeed == null)
            {
                result.Added.Add(feedDeployment);
            }
            else if (existingFeed.Hash != manifestResult.FileHash)
            {
                result.Modified.Add(feedDeployment);
            }
        }

        var scannedFeedKeys = manifests
            .Select(m => m.Item.Name)
            .ToHashSet();

        var removedFeeds = existingFeeds
            .Where(f => !scannedFeedKeys.Contains(f.Name))
            .Select(f => f.Id)
            .ToList();

        result = result with { RemovedIds = removedFeeds };

        return result;
    }

    public bool ValidateFeedManifest(FeedManifest feedManifest)
    {
        return !string.IsNullOrWhiteSpace(feedManifest.Name) &&
               !string.IsNullOrWhiteSpace(feedManifest.Config.Source.NetworkId) &&
               feedManifest.Config.Source.DataType != null;
    }
}
