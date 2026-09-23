namespace Atria.Common.KV.Models;

public sealed record KvBucketListResult
{
    public IReadOnlyList<string> Names { get; init; } = [];

    public int Total { get; init; }

    public bool HasMore { get; init; }

    public string? Cursor { get; init; }
}
