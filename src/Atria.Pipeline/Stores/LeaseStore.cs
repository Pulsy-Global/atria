using Atria.Common.Messaging.Core;
using Atria.Pipeline.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.KeyValueStore;

namespace Atria.Pipeline.Stores;

public sealed class LeaseStore
{
    private readonly NatsConnectionManager _connectionManager;
    private readonly LeaseOptions _options;
    private readonly ILogger<LeaseStore> _logger;

    public record LeaseEntry(string InstanceId);

    public LeaseStore(
        NatsConnectionManager connectionManager,
        IOptions<LeaseOptions> options,
        ILogger<LeaseStore> logger)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> TryAcquireAsync(
        string resourceType,
        string resourceId,
        string instanceId,
        CancellationToken ct)
    {
        var store = await GetStoreAsync(ct);
        var key = GetKey(resourceType, resourceId);

        try
        {
            var entry = new LeaseEntry(instanceId);
            await store.CreateAsync(key, entry, cancellationToken: ct);

            _logger.LogInformation(
                "Acquired lease for {ResourceType}/{ResourceId} by instance {InstanceId}",
                resourceType,
                resourceId,
                instanceId);

            return true;
        }
        catch (NatsKVCreateException)
        {
            _logger.LogDebug(
                "Lease for {ResourceType}/{ResourceId} already held by another instance",
                resourceType,
                resourceId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire lease for {ResourceType}/{ResourceId}", resourceType, resourceId);
            return false;
        }
    }

    public async Task<bool> RenewAsync(
        string resourceType,
        string resourceId,
        string instanceId,
        CancellationToken ct)
    {
        var store = await GetStoreAsync(ct);
        var key = GetKey(resourceType, resourceId);

        try
        {
            var (entry, revision) = await GetLeaseEntryWithRevisionAsync(store, key, ct);

            if (entry is null || entry.InstanceId != instanceId)
            {
                _logger.LogWarning(
                    "Cannot renew lease for {ResourceType}/{ResourceId}: not owned by {InstanceId}",
                    resourceType,
                    resourceId,
                    instanceId);
                return false;
            }

            try
            {
                await store.UpdateAsync(key, entry, revision, cancellationToken: ct);
                _logger.LogDebug("Renewed lease for {ResourceType}/{ResourceId}", resourceType, resourceId);
                return true;
            }
            catch (NatsKVWrongLastRevisionException)
            {
                _logger.LogWarning(
                    "Lost lease for {ResourceType}/{ResourceId} during renewal (revision conflict)",
                    resourceType,
                    resourceId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to renew lease for {ResourceType}/{ResourceId}", resourceType, resourceId);
            return false;
        }
    }

    public async Task ReleaseAsync(
        string resourceType,
        string resourceId,
        string instanceId,
        CancellationToken ct)
    {
        var store = await GetStoreAsync(ct);
        var key = GetKey(resourceType, resourceId);

        try
        {
            var (entry, revision) = await GetLeaseEntryWithRevisionAsync(store, key, ct);

            if (entry is null)
            {
                return;
            }

            if (entry.InstanceId != instanceId)
            {
                _logger.LogWarning(
                    "Cannot release lease for {ResourceType}/{ResourceId}: owned by {OwnerId}, not {InstanceId}",
                    resourceType,
                    resourceId,
                    entry.InstanceId,
                    instanceId);
                return;
            }

            await store.DeleteAsync(key, new NatsKVDeleteOpts { Revision = revision }, cancellationToken: ct);

            _logger.LogInformation(
                "Released lease for {ResourceType}/{ResourceId} by instance {InstanceId}",
                resourceType,
                resourceId,
                instanceId);
        }
        catch (NatsKVKeyNotFoundException)
        {
            // Already deleted (TTL expired)
        }
        catch (NatsKVWrongLastRevisionException)
        {
            _logger.LogDebug(
                "Lease for {ResourceType}/{ResourceId} was modified during release",
                resourceType,
                resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release lease for {ResourceType}/{ResourceId}", resourceType, resourceId);
        }
    }

    public async Task<LeaseEntry?> GetLeaseAsync(
        string resourceType,
        string resourceId,
        CancellationToken ct)
    {
        var store = await GetStoreAsync(ct);
        var key = GetKey(resourceType, resourceId);
        var (entry, _) = await GetLeaseEntryWithRevisionAsync(store, key, ct);
        return entry;
    }

    public async Task<IReadOnlyList<string>> ListOwnedAsync(
        string resourceType,
        string instanceId,
        CancellationToken ct)
    {
        var store = await GetStoreAsync(ct);
        var prefix = $"{resourceType}.";
        var ownedResources = new List<string>();

        try
        {
            await foreach (var key in store.GetKeysAsync([$"{prefix}>"], cancellationToken: ct))
            {
                var (entry, _) = await GetLeaseEntryWithRevisionAsync(store, key, ct);
                if (entry is not null && entry.InstanceId == instanceId)
                {
                    var resourceId = key[prefix.Length..];
                    ownedResources.Add(resourceId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list owned leases for instance {InstanceId}", instanceId);
        }

        return ownedResources;
    }

    private static string GetKey(string resourceType, string resourceId) => $"{resourceType}.{resourceId}";

    private ValueTask<INatsKVStore> GetStoreAsync(CancellationToken ct)
        => _connectionManager.GetKVStoreAsync(_options.BucketName, ct);

    private async Task<(LeaseEntry? Entry, ulong Revision)> GetLeaseEntryWithRevisionAsync(
        INatsKVStore store,
        string key,
        CancellationToken ct)
    {
        try
        {
            var entry = await store.GetEntryAsync<LeaseEntry>(key, cancellationToken: ct);
            return (entry.Value, entry.Revision);
        }
        catch (NatsKVKeyNotFoundException)
        {
            return (null, 0);
        }
        catch (NatsKVKeyDeletedException)
        {
            return (null, 0);
        }
    }
}
