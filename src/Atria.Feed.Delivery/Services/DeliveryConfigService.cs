using Atria.Common.Messaging.RequestReply;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Subjects.Feed;
using Atria.Feed.Delivery.Config.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atria.Feed.Delivery.Services;

public class DeliveryConfigService
{
    private readonly IRequestClient _requestClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeliveryConfigService> _logger;
    private readonly FeedDeliveryOptions _options;
    private readonly TimeSpan _ttlTimeout;

    public DeliveryConfigService(
        IMemoryCache cache,
        ILogger<DeliveryConfigService> logger,
        IOptions<FeedDeliveryOptions> options,
        IRequestClient requestClient)
    {
        _cache = cache;
        _logger = logger;
        _options = options.Value;
        _requestClient = requestClient;
        _ttlTimeout = TimeSpan.FromMinutes(_options.ConfigCache.SlidingExpirationMinutes);

        _logger.LogInformation(
            "DeliveryConfigService initialized (Cache TTL: {TTL} min)",
            _options.ConfigCache.SlidingExpirationMinutes);
    }

    public static string GetCacheKey(string feedId) => $"config_{feedId}";

    public async Task<FeedDeliveryTarget?> TryGetTargetById(string id, CancellationToken ct)
    {
        try
        {
            return await GetConfigById(id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TryGetTargetById failed for id={Id}", id);
            return null;
        }
    }

    public async Task<FeedDeliveryTarget?> GetConfigById(string id, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var cacheKey = GetCacheKey(id);

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = _ttlTimeout;

            try
            {
                var config = await LoadConfigFromNats(id, ct);
                if (config == null)
                {
                    _logger.LogWarning("No delivery pipeline found for id={Id}", id);
                    throw new InvalidDataException($"Delivery pipeline not found for id={id}");
                }

                _logger.LogDebug("Successfully loaded config for id={Id}", id);
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load delivery config for id={Id}", id);
                throw;
            }
        });
    }

    private async Task<FeedDeliveryTarget?> LoadConfigFromNats(string id, CancellationToken ct)
    {
        return await _requestClient.SendAsync<FeedDeliveryTargetRequest, FeedDeliveryTarget>(
            FeedSubjects.System.DeliveryConfigRequest,
            new FeedDeliveryTargetRequest(Id: id),
            timeout: TimeSpan.FromSeconds(30),
            ct: ct);
    }
}
