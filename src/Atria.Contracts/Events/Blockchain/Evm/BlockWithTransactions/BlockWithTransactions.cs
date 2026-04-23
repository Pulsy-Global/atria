using Atria.Contracts.Events.Blockchain.Evm.BlockWithTransactions.Models;
using Atria.Contracts.Events.Blockchain.Evm.Common;
using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.BlockWithTransactions;

public sealed record BlockWithTransactions(
    [property: JsonPropertyName("metadata")]
    Metadata Metadata,
    [property: JsonPropertyName("block")]
    BlockWithTransactionData Block);
