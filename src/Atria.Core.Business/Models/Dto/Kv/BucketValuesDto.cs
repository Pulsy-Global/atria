namespace Atria.Core.Business.Models.Dto.Kv;

public class BucketValuesDto
{
    public IReadOnlyList<BucketItemDto> Items { get; init; } = [];

    public string? Cursor { get; init; }

    public bool HasMore { get; init; }
}
