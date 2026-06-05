using System.Collections.ObjectModel;

namespace Atria.Core.Business.Models.Dto.Kv;

public class AddBucketBatchDto
{
    public List<BucketItemDto> Items { get; set; } = [];

    public IReadOnlyDictionary<string, string> ToReadOnlyItems()
        => new ReadOnlyDictionary<string, string>(
            Items.DistinctBy(o => o.Key).ToDictionary(o => o.Key, o => o.Value));
}
