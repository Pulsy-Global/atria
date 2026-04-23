using Atria.Contracts.Events.Blockchain.Evm.BlockWithLogs.Models;
using Atria.Contracts.Events.Blockchain.Evm.BlockWithTransactions.Models;
using Atria.Contracts.Events.Blockchain.Evm.DebugTrace.Models;
using Atria.Contracts.Events.Blockchain.Evm.DebugTrace.Models.Response;
using Mapster;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using BlockWithTransactions = Nethereum.RPC.Eth.DTOs.BlockWithTransactions;

namespace Atria.Feed.Ingestor.Mapper.Mappings;

public class IngestorMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<HexBigInteger?, string>()
            .MapWith(src => src == null ? "0x0" : src.HexValue);

        config.NewConfig<BlockWithTransactions, BlockWithTransactionData>()
            .Map(dest => dest.TransactionsInfo, src => src.Transactions)
            .Map(dest => dest.Number, src => src.Number)
            .Map(dest => dest.Hash, src => src.BlockHash)
            .Map(dest => dest.Author, src => src.Author)
            .Map(dest => dest.SealFields, src => src.SealFields)
            .Map(dest => dest.ParentHash, src => src.ParentHash)
            .Map(dest => dest.Nonce, src => src.Nonce)
            .Map(dest => dest.Sha3Uncles, src => src.Sha3Uncles)
            .Map(dest => dest.LogsBloom, src => src.LogsBloom)
            .Map(dest => dest.TransactionsRoot, src => src.TransactionsRoot)
            .Map(dest => dest.StateRoot, src => src.StateRoot)
            .Map(dest => dest.ReceiptsRoot, src => src.ReceiptsRoot)
            .Map(dest => dest.Miner, src => src.Miner)
            .Map(dest => dest.Difficulty, src => src.Difficulty)
            .Map(dest => dest.TotalDifficulty, src => src.TotalDifficulty)
            .Map(dest => dest.MixHash, src => src.MixHash)
            .Map(dest => dest.ExtraData, src => src.ExtraData)
            .Map(dest => dest.Size, src => src.Size)
            .Map(dest => dest.GasLimit, src => src.GasLimit)
            .Map(dest => dest.GasUsed, src => src.GasUsed)
            .Map(dest => dest.Timestamp, src => src.Timestamp)
            .Map(dest => dest.Uncles, src => src.Uncles)
            .Map(dest => dest.BaseFeePerGas, src => src.BaseFeePerGas)
            .Map(dest => dest.WithdrawalsRoot, src => src.WithdrawalsRoot)
            .Map(dest => dest.Withdrawals, src => src.Withdrawals);

        config.NewConfig<Transaction, TransactionsInfoData>()
            .Map(dest => dest.Hash, src => src.TransactionHash)
            .Map(dest => dest.TransactionIndex, src => src.TransactionIndex)
            .Map(dest => dest.Type, src => src.Type)
            .Map(dest => dest.BlockHash, src => src.BlockHash)
            .Map(dest => dest.BlockNumber, src => src.BlockNumber)
            .Map(dest => dest.From, src => src.From)
            .Map(dest => dest.To, src => src.To)
            .Map(dest => dest.Gas, src => src.Gas)
            .Map(dest => dest.GasPrice, src => src.GasPrice)
            .Map(dest => dest.MaxFeePerGas, src => src.MaxFeePerGas)
            .Map(dest => dest.Value, src => src.Value)
            .Map(dest => dest.Input, src => src.Input)
            .Map(dest => dest.Nonce, src => src.Nonce)
            .Map(dest => dest.R, src => src.R)
            .Map(dest => dest.S, src => src.S)
            .Map(dest => dest.V, src => src.V)
            .Map(dest => dest.AccessList, src => src.AccessList)
            .Map(dest => dest.AuthorizationList, src => src.AuthorisationList);

        config.NewConfig<Withdrawal, WithdrawalData>()
            .Map(dest => dest.Index, src => src.Index)
            .Map(dest => dest.ValidatorIndex, src => src.ValidatorIndex)
            .Map(dest => dest.Address, src => src.Address)
            .Map(dest => dest.Amount, src => src.Amount);

        config.NewConfig<AccessList, AccessListData>()
            .Map(dest => dest.Address, src => src.Address)
            .Map(dest => dest.StorageKeys, src => src.StorageKeys);

        config.NewConfig<FilterLog, EvmLogData>()
            .Map(dest => dest.Removed, src => src.Removed)
            .Map(dest => dest.Type, src => src.Type)
            .Map(dest => dest.LogIndex, src => src.LogIndex.HexValue)
            .Map(dest => dest.TransactionHash, src => src.TransactionHash)
            .Map(dest => dest.TransactionIndex, src => src.TransactionIndex)
            .Map(dest => dest.BlockHash, src => src.BlockHash)
            .Map(dest => dest.BlockNumber, src => src.BlockNumber)
            .Map(dest => dest.Address, src => src.Address)
            .Map(dest => dest.Data, src => src.Data)
            .Map(dest => dest.Topics, src => src.Topics);

        config.NewConfig<ResultElementResponse, DebugTraceData>()
            .Map(dest => dest.TxHash, src => src.TxHash)
            .Map(dest => dest.Result, src => src.Result);

        config.NewConfig<ResultResponse, ResultData>()
            .Map(dest => dest.From, src => src.From)
            .Map(dest => dest.Gas, src => src.Gas)
            .Map(dest => dest.GasUsed, src => src.GasUsed)
            .Map(dest => dest.Input, src => src.Input)
            .Map(dest => dest.To, src => src.To)
            .Map(dest => dest.Type, src => src.Type)
            .Map(dest => dest.Value, src => src.Value)
            .Map(dest => dest.Calls, src => src.Calls)
            .Map(dest => dest.Output, src => src.Output)
            .Map(dest => dest.Error, src => src.Error);

        config.NewConfig<ResultCallResponse, ResultCallData>()
            .Map(dest => dest.From, src => src.From)
            .Map(dest => dest.Gas, src => src.Gas)
            .Map(dest => dest.GasUsed, src => src.GasUsed)
            .Map(dest => dest.Input, src => src.Input)
            .Map(dest => dest.To, src => src.To)
            .Map(dest => dest.Type, src => src.Type)
            .Map(dest => dest.Value, src => src.Value)
            .Map(dest => dest.Calls, src => src.Calls)
            .Map(dest => dest.Output, src => src.Output)
            .Map(dest => dest.Error, src => src.Error);

        config.NewConfig<CallResponse, CallData>()
            .Map(dest => dest.From, src => src.From)
            .Map(dest => dest.Gas, src => src.Gas)
            .Map(dest => dest.GasUsed, src => src.GasUsed)
            .Map(dest => dest.Input, src => src.Input)
            .Map(dest => dest.To, src => src.To)
            .Map(dest => dest.Type, src => src.Type)
            .Map(dest => dest.Value, src => src.Value)
            .Map(dest => dest.Calls, src => src.Calls)
            .Map(dest => dest.Output, src => src.Output);
    }
}
