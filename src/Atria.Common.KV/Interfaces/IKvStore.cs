using Atria.Common.KV.Models;

namespace Atria.Common.KV.Interfaces;

public interface IKvStore
{
    Task BucketAddAsync(string name, string key, string value);

    Task BucketAddBatchAsync(string name, IReadOnlyDictionary<string, string> items);

    Task<string?> BucketGetAsync(string name, string key);

    Task<IReadOnlyDictionary<string, string>> BucketGetBatchAsync(string name, IReadOnlyList<string> keys);

    Task BucketRemoveAsync(string name, string key);

    Task BucketRemoveBatchAsync(string name, IReadOnlyList<string> keys);

    Task<KvBucketValuesResult> BucketValuesAsync(string name, int limit, string? cursor);
}
