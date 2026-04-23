using Atria.Contracts.Events.Blockchain.Evm.BlockWithLogs;
using Atria.Contracts.Events.Blockchain.Evm.BlockWithTransactions;
using Atria.Contracts.Events.Blockchain.Evm.DebugTrace;

namespace Atria.Feed.Ingestor.ChainClients.Models;

public class BlockData
{
    public BlockWithTransactions Block { get; init; }

    public BlockWithLogs Logs { get; init; }

    public DebugTraces? Traces { get; init; }
}
