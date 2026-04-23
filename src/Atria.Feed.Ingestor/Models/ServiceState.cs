using System.Numerics;

namespace Atria.Feed.Ingestor.Models;

public record ServiceState
{
    public BigInteger LastProcessedBlock { get; init; }

    public BigInteger CurrentChainBlock { get; init; }

    public bool HasMissedBlocks { get; init; }

    public BigInteger MissedBlockCount { get; init; }
}
