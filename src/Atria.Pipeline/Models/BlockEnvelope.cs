using System.Numerics;

namespace Atria.Pipeline.Models;

public sealed record BlockEnvelope(
    BigInteger BlockNumber,
    string? BlockHash,
    string DataType,
    object? Data);
