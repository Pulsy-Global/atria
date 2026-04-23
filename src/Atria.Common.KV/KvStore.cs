using Atria.Common.KV.Interfaces;
using Atria.Common.KV.Models;
using Pulsy.EKV.Client.Namespaces;
using System.Text;

namespace Atria.Common.KV;

public class KvStore : IKvStore
{
    private const string BucketPrefix = "B:";

    private readonly IEkvNamespace _namespace;

    public KvStore(IEkvNamespace ns)
    {
        _namespace = ns;
    }

    public async Task BucketAddAsync(string name, string key, string value)
    {
        await _namespace.BatchAsync(batch =>
        {
            batch.Put(FormatKey(name, key), ToBytes(value));
        });
    }

    public async Task BucketAddBatchAsync(string name, IReadOnlyDictionary<string, string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        await _namespace.BatchAsync(batch =>
        {
            foreach (var (item, value) in items)
            {
                batch.Put(FormatKey(name, item), ToBytes(value));
            }
        });
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
        await _namespace.BatchAsync(batch =>
        {
            batch.Delete(FormatKey(name, key));
        });
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
}
