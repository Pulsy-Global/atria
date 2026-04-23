using Atria.Contracts.Events.Blockchain.Evm.BlockWithLogs;
using Atria.Contracts.Events.Blockchain.Evm.BlockWithTransactions;
using Atria.Contracts.Events.Blockchain.Evm.DebugTrace;
using Atria.Feed.Ingestor.ChainClients.Models;
using System.Numerics;

namespace Atria.Feed.Ingestor.ChainClients.Interfaces;

public interface IEvmHttpClient
{
    Task<BlockData> FetchBlockAllDataAsync(BigInteger blockNumber, CancellationToken ct);
    Task<BigInteger> GetLatestBlockNumberAsync(CancellationToken ct);
    Task<BlockWithTransactions> GetBlockWithTransactionsAsync(BigInteger blockNumber, CancellationToken ct);
    Task<BlockWithLogs> GetBlockWithLogsAsync(BigInteger blockNumber, CancellationToken ct);
    Task<DebugTraces> GetDebugTracesAsync(BigInteger blockNumber, CancellationToken ct);
}
