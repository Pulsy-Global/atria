namespace Atria.Core.Business.Models.Dto.Kv;

public class BucketBatchKeysDto
{
    public IReadOnlyList<string> Keys { get; set; }

    public BucketBatchKeysDto()
    {
        Keys = [];
    }

    public BucketBatchKeysDto(string keys)
    {
        Keys = [.. keys.Split(',', StringSplitOptions.RemoveEmptyEntries)];
    }
}
