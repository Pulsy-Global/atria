namespace Atria.Common.KV.Models;

public sealed record KvBucketEntry
{
    public string Key { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}
