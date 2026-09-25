using Atria.Common.KV.Interfaces;
using Atria.Common.KV.Models;
using Pulsy.EKV.Client.Namespaces;
using System.Text;

namespace Atria.Common.KV;

public class KvStore : IKvStore
{
    private const string BucketPrefix = "B:";

    private const string RegistryPrefix = "R:";

    private static readonly byte[] RegistryMarkerValue = [1];

    private readonly IEkvNamespace _namespace;

    private readonly object _knownBucketsLock = new();

    private readonly HashSet<string> _knownBuckets = new(StringComparer.Ordinal);

    public KvStore(IEkvNamespace ns)
    {
        _namespace = ns;
    }

    public async Task BucketAddAsync(string name, string key, string value)
    {
        var dataKey = FormatKey(name, key);
        var registryKey = RegistryKey(name);
        var register = !IsKnownBucket(name);

        await _namespace.BatchAsync(batch =>
        {
            batch.Put(dataKey, ToBytes(value));

            if (register)
            {
                batch.Put(registryKey, RegistryMarkerValue);
            }
        });

        RememberBucket(name);
    }

    public async Task BucketAddBatchAsync(string name, IReadOnlyDictionary<string, string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrEmpty(name);

        if (name.Contains(':'))
        {
            throw new ArgumentException("Bucket name must not contain ':'", nameof(name));
        }

        var registryKey = RegistryKey(name);
        var register = !IsKnownBucket(name);

        await _namespace.BatchAsync(batch =>
        {
            foreach (var (item, value) in items)
            {
                batch.Put(FormatKey(name, item), ToBytes(value));
            }

            if (register)
            {
                batch.Put(registryKey, RegistryMarkerValue);
            }
        });

        RememberBucket(name);
    }

    public async Task<string?> BucketGetAsync(string name, string key)
    {
        var result = await _namespace.GetAsync(FormatKey(name, key));
        return FromBytes(result);
    }

    public async Task<IReadOnlyDictionary<string, string>> BucketGetBatchAsync(string name, IReadOnlyList<string> keys)
    {
        if (keys.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var formattedKeys = keys.Distinct().Select(i => FormatKey(name, i)).ToList();
        var result = await _namespace.MultiGetAsync(formattedKeys);

        var prefix = BucketPrefix + name + ":";

        return result.ToDictionary(
            kv => kv.Key[prefix.Length..],
            kv => FromBytes(kv.Value) ?? string.Empty);
    }

    public async Task BucketRemoveAsync(string name, string key)
    {
        var dataKey = FormatKey(name, key);

        await _namespace.BatchAsync(batch => batch.Delete(dataKey));
    }

    public async Task BucketRemoveBatchAsync(string name, IReadOnlyList<string> keys)
    {
        if (keys.Count == 0)
        {
            return;
        }

        await _namespace.BatchAsync(batch =>
        {
            foreach (var key in keys)
            {
                batch.Delete(FormatKey(name, key));
            }
        });
    }

    public async Task<KvBucketValuesResult> BucketValuesAsync(string name, int limit, string? cursor)
    {
        var prefix = BucketPrefix + name + ":";
        var result = await _namespace.ScanPrefixAsync(
            prefix,
            limit,
            string.IsNullOrEmpty(cursor) ? null : cursor);

        var items = result.Items
            .Select(e => new KvBucketEntry
            {
                Key = e.Key[prefix.Length..],
                Value = FromBytes(e.Value) ?? string.Empty,
            })
            .ToList();

        return new KvBucketValuesResult
        {
            Items = items,
            Cursor = result.NextCursor,
            HasMore = result.HasMore,
        };
    }

    public async Task<KvBucketListResult> ListBucketsAsync(int limit, string? cursor = null)
    {
        var result = await _namespace.ScanPrefixAsync(
            RegistryPrefix,
            limit,
            string.IsNullOrEmpty(cursor) ? null : cursor);

        var names = result.Items
            .Where(e => e.Key.Length > RegistryPrefix.Length)
            .Select(e => e.Key[RegistryPrefix.Length..])
            .Distinct(StringComparer.Ordinal)
            .ToList();

        names.ForEach(RememberBucket);

        return new KvBucketListResult
        {
            Names = names,
            Total = names.Count,
            HasMore = result.HasMore,
            Cursor = result.NextCursor,
        };
    }

    private static string RegistryKey(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (name.Contains(':'))
        {
            throw new ArgumentException("Bucket name must not contain ':'", nameof(name));
        }

        return RegistryPrefix + name;
    }

    private static string FormatKey(string name, string item)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(item);

        if (name.Contains(':'))
        {
            throw new ArgumentException("Bucket name must not contain ':'", nameof(name));
        }

        if (item.Contains(':'))
        {
            throw new ArgumentException("Item key must not contain ':'", nameof(item));
        }

        return $"{BucketPrefix}{name}:{item}";
    }

    private static byte[] ToBytes(string value)
        => string.IsNullOrEmpty(value) ? [] : Encoding.UTF8.GetBytes(value);

    private static string? FromBytes(byte[]? bytes)
        => bytes is { Length: > 0 } ? Encoding.UTF8.GetString(bytes) : null;

    private bool IsKnownBucket(string name)
    {
        lock (_knownBucketsLock)
        {
            return _knownBuckets.Contains(name);
        }
    }

    private void RememberBucket(string name)
    {
        lock (_knownBucketsLock)
        {
            _knownBuckets.Add(name);
        }
    }
}
