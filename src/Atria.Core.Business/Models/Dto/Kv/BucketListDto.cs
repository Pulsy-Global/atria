namespace Atria.Core.Business.Models.Dto.Kv;

public class BucketListDto
{
    public IReadOnlyList<string> Buckets { get; init; } = [];

    public int Total { get; init; }

    public bool HasMore { get; init; }

    public string? Cursor { get; init; }
}
