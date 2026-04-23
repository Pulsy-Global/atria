namespace Atria.Feed.Runtime.Engine.Filters.Interfaces;

public interface IKvHostBridge
{
    Task BucketAddAsync(string name, string key, string? valueJson);

    Task BucketAddBatchAsync(string name, string itemsJson);

    Task<string?> BucketGetAsync(string name, string item);

    Task<string> BucketGetBatchAsync(string name, string itemsJson);

    Task BucketRemoveAsync(string name, string key);

    Task BucketRemoveBatchAsync(string name, string itemsJson);

    Task<string> BucketValuesAsync(string name, int limit, string cursor);
}
