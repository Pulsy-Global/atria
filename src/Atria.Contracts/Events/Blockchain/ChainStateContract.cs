namespace Atria.Contracts.Events.Blockchain;

public sealed record ChainHeadRequest(string ChainId);

public sealed record ChainHeadResponse(bool Success, ulong? BlockNumber, string? Error = null);
