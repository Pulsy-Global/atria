using Pulsy.EKV.Client.Models;
using Pulsy.EKV.Client.Namespaces;
using Pulsy.EKV.Client.Operations;
using Pulsy.EKV.Grpc;
using System.Runtime.CompilerServices;

namespace Atria.Common.KV.Tests.Fakes;

internal sealed class FakeEkvNamespace : IEkvNamespace
{
    private readonly SortedDictionary<string, byte[]> _data = new(StringComparer.Ordinal);
    private readonly List<List<BatchOp>> _batches = new();

    public IReadOnlyDictionary<string, byte[]> Data => _data;

    public List<List<BatchOp>> Batches => _batches;

    public int GetCalls { get; private set; }

    public int MultiGetCalls { get; private set; }

    public int ScanCalls { get; private set; }

    public int WriteOpCount => _batches.Sum(b => b.Count);

    public void Seed(string key, byte[] value) => _data[key] = value;

    public Task<byte[]?> GetAsync(string key, CancellationToken ct = default)
    {
        GetCalls++;
        return Task.FromResult(_data.TryGetValue(key, out var value) ? value : null);
    }

    public Task PutAsync(string key, byte[] value, CancellationToken ct = default)
    {
        var batch = new List<BatchOp> { NewOp(key, BatchOpType.Put, value) };
        Apply(batch);
        return Task.CompletedTask;
    }

    public Task PutAsync(string key, byte[] value, TimeSpan ttl, CancellationToken ct = default)
        => PutAsync(key, value, ct);

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var batch = new List<BatchOp> { NewOp(key, BatchOpType.Delete, null) };
        Apply(batch);
        return Task.CompletedTask;
    }

    public Task BatchAsync(Action<IBatchBuilder> configure, CancellationToken ct = default)
    {
        var builder = new FakeBatchBuilder();
        configure(builder);
        Apply(builder.Ops);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, byte[]>> MultiGetAsync(
        IReadOnlyList<string> keys,
        CancellationToken ct = default)
    {
        MultiGetCalls++;
        var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            if (_data.TryGetValue(key, out var value))
            {
                result[key] = value;
            }
        }

        return Task.FromResult<IReadOnlyDictionary<string, byte[]>>(result);
    }

    public Task<ScanResult> ScanPrefixAsync(
        string prefix,
        int limit,
        string? cursor,
        CancellationToken ct = default)
    {
        ScanCalls++;

        var matching = _data
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(kv => new KvEntry { Key = kv.Key, Value = kv.Value })
            .ToList();

        var start = 0;
        if (!string.IsNullOrEmpty(cursor))
        {
            var index = matching.FindIndex(e => string.Equals(e.Key, cursor, StringComparison.Ordinal));
            start = index < 0 ? 0 : index + 1;
        }

        var page = matching.Skip(start).Take(limit).ToList();
        var hasMore = start + page.Count < matching.Count;

        return Task.FromResult(new ScanResult
        {
            Items = page,
            NextCursor = hasMore && page.Count > 0 ? page[^1].Key : null,
            HasMore = hasMore,
        });
    }

    public async IAsyncEnumerable<KvEntry> ScanPrefixAllAsync(
        string prefix,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? cursor = null;
        do
        {
            var page = await ScanPrefixAsync(prefix, 100, cursor, ct);
            foreach (var entry in page.Items)
            {
                yield return entry;
            }

            cursor = page.NextCursor;
        }
        while (cursor is not null);
    }

    private static BatchOp NewOp(string key, BatchOpType type, byte[]? value)
        => new(key, type, value, null);

    private void Apply(List<BatchOp> ops)
    {
        _batches.Add(ops);
        foreach (var op in ops)
        {
            switch (op.Type)
            {
                case BatchOpType.Put:
                    _data[op.Key] = op.Value ?? [];
                    break;
                case BatchOpType.Delete:
                    _data.Remove(op.Key);
                    break;
            }
        }
    }

    private sealed class FakeBatchBuilder : IBatchBuilder
    {
        public List<BatchOp> Ops { get; } = new();

        public void Put(string key, byte[] value)
            => Ops.Add(NewOp(key, BatchOpType.Put, value));

        public void Put(string key, byte[] value, TimeSpan ttl)
            => Ops.Add(NewOp(key, BatchOpType.Put, value));

        public void Delete(string key)
            => Ops.Add(NewOp(key, BatchOpType.Delete, null));
    }
}
