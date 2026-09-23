namespace Atria.Core.Business.Models.Dto.Kv;

public class BucketListDto
{
    public IReadOnlyList<string> Buckets { get; init; } = [];

    public int Total { get; init; }

    public bool HasMore { get; init; }

    // Opaque cursor for fetching the next page of buckets; null when exhausted.
    public string? Cursor { get; init; }
}
