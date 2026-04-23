using Atria.Contracts.Events.Blockchain.Evm.BlockWithLogs.Models;
using Atria.Contracts.Events.Blockchain.Evm.Common;
using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.BlockWithLogs;

public sealed record BlockWithLogs(
    [property: JsonPropertyName("metadata")]
    Metadata Metadata,
    [property: JsonPropertyName("logs")]
    List<EvmLogData> Logs);
