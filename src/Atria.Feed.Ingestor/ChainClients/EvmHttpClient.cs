using Atria.Common.Models.Options;
using Atria.Contracts.Events.Blockchain.Evm.BlockWithLogs;
using Atria.Contracts.Events.Blockchain.Evm.BlockWithLogs.Models;
using Atria.Contracts.Events.Blockchain.Evm.BlockWithTransactions.Models;
using Atria.Contracts.Events.Blockchain.Evm.Common;
using Atria.Contracts.Events.Blockchain.Evm.DebugTrace;
using Atria.Contracts.Events.Blockchain.Evm.DebugTrace.Models;
using Atria.Contracts.Events.Blockchain.Evm.DebugTrace.Models.Response;
using Atria.Feed.Ingestor.ChainClients.Helpers;
using Atria.Feed.Ingestor.ChainClients.Interfaces;
using Atria.Feed.Ingestor.ChainClients.Models;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;
using System.Numerics;
using BlockWithTransactions = Atria.Contracts.Events.Blockchain.Evm.BlockWithTransactions.BlockWithTransactions;

namespace Atria.Feed.Ingestor.ChainClients;

public class EvmHttpClient : IEvmHttpClient
{
    private readonly NetworkOptions _chainOptions;
    private readonly IMapper _mapper;
    private readonly ILogger<EvmHttpClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly Web3 _web3Client;

    public EvmHttpClient(
        NetworkOptions chainOptions,
        IMapper mapper,
        ILogger<EvmHttpClient> logger,
        HttpClient httpClient)
    {
        _mapper = mapper;
        _logger = logger;
        _chainOptions = chainOptions;
        _httpClient = httpClient;

        var rpcClient = new RpcClient(new Uri(_chainOptions.NodeRpcUrl), _httpClient);
        _web3Client = new Web3(rpcClient);
    }

    public async Task<BlockData> FetchBlockAllDataAsync(BigInteger blockNumber, CancellationToken ct)
    {
        _logger.LogDebug("Getting block data for {BlockNumber} on {Chain}", blockNumber, _chainOptions.Id);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

        var blockTask = GetBlockWithTransactionsAsync(blockNumber, timeoutCts.Token);
        var logsTask = GetBlockWithLogsAsync(blockNumber, timeoutCts.Token);
        var debugTask = _chainOptions.DebugRequestsEnabled
            ? GetDebugTracesAsync(blockNumber, timeoutCts.Token)
            : Task.FromResult<DebugTraces>(null!);

        try
        {
            await Task.WhenAll(blockTask, logsTask, debugTask);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Block data fetching timed out for block {BlockNumber} on {Chain}", blockNumber, _chainOptions.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error fetching block data for block {BlockNumber} on {Chain}", blockNumber, _chainOptions.Id);
            throw;
        }

        return new BlockData { Block = await blockTask, Logs = await logsTask, Traces = await debugTask, };
    }

    public async Task<BigInteger> GetLatestBlockNumberAsync(CancellationToken ct)
    {
        var latestBlockNumber = await _web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        return latestBlockNumber.Value;
    }

    public async Task<BlockWithTransactions> GetBlockWithTransactionsAsync(BigInteger blockNumber, CancellationToken ct)
    {
        var result = await _web3Client.Eth.Blocks.GetBlockWithTransactionsByNumber
            .SendRequestAsync(new HexBigInteger(blockNumber));

        if (result == null)
        {
            throw new InvalidOperationException($"Block {blockNumber} not found");
        }

        var mappedBlockData = _mapper.Map<BlockWithTransactionData>(result);
        var metadata = new Metadata
        {
            NetworkId = _chainOptions.Id,
            BlockNumber = blockNumber.ToString(),
        };

        return new BlockWithTransactions(metadata, mappedBlockData);
    }

    public async Task<BlockWithLogs> GetBlockWithLogsAsync(BigInteger blockNumber, CancellationToken ct)
    {
        var hexBlockNumber = new HexBigInteger(blockNumber);
        var filter = new NewFilterInput
        {
            FromBlock = new BlockParameter(hexBlockNumber),
            ToBlock = new BlockParameter(hexBlockNumber),
        };

        var filterLogs = await _web3Client.Eth.Filters.GetLogs.SendRequestAsync(filter);
        var mappedLogs = filterLogs.Adapt<List<EvmLogData>>();

        var metadata = new Metadata
        {
            NetworkId = _chainOptions.Id,
            BlockNumber = blockNumber.ToString(),
        };

        return new BlockWithLogs(metadata, mappedLogs);
    }

    public async Task<DebugTraces> GetDebugTracesAsync(BigInteger blockNumber, CancellationToken ct)
    {
        var rpcRequest = DebugTraceRpcRequest.BuildDebugTraceRpcRequest(blockNumber);

        var requestMessage = DebugTraceRpcRequest.BuildDebugTraceHttpRequestMessage(_chainOptions.NodeRpcUrl, rpcRequest);

        using var response = await _httpClient.SendAsync(requestMessage, ct);

        var responseResult = await response.Content.ReadAsStringAsync(ct);

        var debugTraceResponse = JsonConvert.DeserializeObject<DebugTraceResponse>(responseResult);

        if (debugTraceResponse?.Error != null)
        {
            throw new InvalidOperationException(debugTraceResponse.Error.Message);
        }

        var mappedDebugTraceData = debugTraceResponse?.Result.Adapt<List<DebugTraceData>>() ?? [];

        var metadata = new Metadata
        {
            NetworkId = _chainOptions.Id,
            BlockNumber = blockNumber.ToString(),
        };

        return new DebugTraces(metadata, mappedDebugTraceData);
    }
}
