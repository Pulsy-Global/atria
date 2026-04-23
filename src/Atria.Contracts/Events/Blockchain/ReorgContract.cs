namespace Atria.Contracts.Events.Blockchain;

public sealed record ReorgEvent(
    string ChainId,
    ulong FromBlock,
    ulong ToBlock);
