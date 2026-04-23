using System.Numerics;

namespace Atria.Common.Exceptions;

public class CursorBehindTailException : BaseException
{
    public CursorBehindTailException(BigInteger feedCursor, BigInteger chainTail)
        : base($"Feed cursor (block {feedCursor}) is behind chain tail (block {chainTail}).")
    {
    }
}
