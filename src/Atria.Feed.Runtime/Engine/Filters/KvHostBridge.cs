using Atria.Common.KV.Interfaces;
using Atria.Feed.Runtime.Engine.Filters.Interfaces;
using Grpc.Core;
using System.Text.Json;

namespace Atria.Feed.Runtime.Engine.Filters;

public class KvHostBridge : IKvHostBridge
{
    private readonly IKvStore _kvStore;

    public KvHostBridge(IKvStore kvStore)
    {
        _kvStore = kvStore;
    }

    public async Task BucketAddAsync(string name, string key, string? valueJson)
    {
        try
        {
            await _kvStore.BucketAddAsync(name, key, valueJson ?? string.Empty);
        }
        catch (RpcException ex)
        {
            throw new InvalidOperationException($"KV bucket add failed for '{name}:{key}': {ex.Status.Detail}", ex);
        }
    }

    public async Task BucketAddBatchAsync(string name, string itemsJson)
    {
        try
        {
            var items = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(itemsJson)
                ?? throw new ArgumentException("Invalid items JSON");

            var entries = new Dictionary<string, string>(items.Count);
            foreach (var (key, val) in items)
            {
                entries[key] = val.ValueKind == JsonValueKind.Null
                    ? string.Empty
                    : val.GetRawText();
            }

            await _kvStore.BucketAddBatchAsync(name, entries);
        }
        catch (RpcException ex)
        {
            throw new InvalidOperationException($"KV bucket addBatch failed for '{name}': {ex.Status.Detail}", ex);
        }
    }

    public async Task<string?> BucketGetAsync(string name, string item)
    {
        try
        {
            return await _kvStore.BucketGetAsync(name, item);
        }
        catch (RpcException ex)
        {
            throw new InvalidOperationException($"KV bucket get failed for '{name}:{item}': {ex.Status.Detail}", ex);
        }
    }

    public async Task<string> BucketGetBatchAsync(string name, string itemsJson)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(itemsJson)
                ?? throw new ArgumentException("Invalid items JSON array");

            var result = await _kvStore.BucketGetBatchAsync(name, items);

            var entries = result.ToDictionary(
                kv => kv.Key,
                kv => string.IsNullOrEmpty(kv.Value) ? null : (object?)JsonSerializer.Deserialize<JsonElement>(kv.Value));

            return JsonSerializer.Serialize(entries);
        }
        catch (RpcException ex)
        {
            throw new InvalidOperationException($"KV bucket getBatch failed for '{name}': {ex.Status.Detail}", ex);
        }
    }

    public async Task BucketRemoveAsync(string name, string key)
    {
        try
        {
            await _kvStore.BucketRemoveAsync(name, key);
        }
        catch (RpcException ex)
        {
            throw new InvalidOperationException($"KV bucket remove failed for '{name}:{key}': {ex.Status.Detail}", ex);
        }
    }

    public async Task BucketRemoveBatchAsync(string name, string itemsJson)
    {
        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(itemsJson)
                ?? throw new ArgumentException("Invalid items JSON array");

            await _kvStore.BucketRemoveBatchAsync(name, items);
        }
        catch (RpcException ex)
        {
            throw new InvalidOperationException($"KV bucket removeBatch failed for '{name}': {ex.Status.Detail}", ex);
        }
    }

    public async Task<string> BucketValuesAsync(string name, int limit, string cursor)
    {
        try
        {
            var result = await _kvStore.BucketValuesAsync(
                name,
                limit,
                string.IsNullOrEmpty(cursor) ? null : cursor);

            var items = result.Items.Select(e => new
            {
                key = e.Key,
                value = string.IsNullOrEmpty(e.Value) ? null : (object?)JsonSerializer.Deserialize<JsonElement>(e.Value),
            });

            return JsonSerializer.Serialize(new
            {
                items,
                cursor = result.Cursor,
                hasMore = result.HasMore,
            });
        }
        catch (RpcException ex)
        {
            throw new InvalidOperationException($"KV bucket values failed for '{name}': {ex.Status.Detail}", ex);
        }
    }
}
