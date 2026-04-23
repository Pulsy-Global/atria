using Atria.Pipeline.Models;
using System.Numerics;

namespace Atria.Pipeline.Interfaces;

public interface IBlockProvider
{
    IAsyncEnumerable<BlockEnvelope> StreamBlocksAsync(
        string chainId,
        string dataType,
        BigInteger fromBlock,
        int blockDelay,
        CancellationToken ct);

    Task<T?> GetBlockAsync<T>(
        string chainId,
        string dataType,
        BigInteger blockNumber,
        CancellationToken ct)
        where T : class;

    Task<BigInteger> GetHeadAsync(string chainId, CancellationToken ct);
}
