using System.Numerics;

namespace Atria.Pipeline.Interfaces;

public interface IFeedCursorStore
{
    Task<BigInteger?> GetAsync(string feedId, CancellationToken ct);

    Task SetAsync(string feedId, BigInteger cursor, CancellationToken ct);

    Task DeleteAsync(string feedId, CancellationToken ct);
}
