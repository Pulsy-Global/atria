namespace Atria.Common.KV.Models;

public sealed record KvBucketListResult
{
    public IReadOnlyList<string> Names { get; init; } = [];

    public int Total { get; init; }

    public bool HasMore { get; init; }

    // Opaque position for fetching the next page; null when the scan finished.
    public string? Cursor { get; init; }
}
