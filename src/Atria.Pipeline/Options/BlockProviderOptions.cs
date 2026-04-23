namespace Atria.Pipeline.Options;

public sealed class BlockProviderOptions
{
    public const string SectionName = "BlockProvider";

    public string BlocksBucketPrefix { get; set; } = "blocks";

    public string ChainStateBucket { get; set; } = "chain-state";

    public string CursorsBucket { get; set; } = "feed-cursors";

    public int DefaultBlocksMaxAgeMinutes { get; set; } = 720;

    public long DefaultBlocksMaxSizeMb { get; set; } = 8192;

    public bool DefaultBlocksCompression { get; set; } = true;

    public int? MaxBlocksCount { get; set; }
}
