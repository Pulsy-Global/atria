namespace Atria.Common.KV.Models;

public sealed record KvBucketValuesResult
{
    public IReadOnlyList<KvBucketEntry> Items { get; init; } = [];

    public string? Cursor { get; init; }

    public bool HasMore { get; init; }
}
