using Atria.Contracts.Events.Blockchain.Evm.Common;
using Atria.Contracts.Events.Blockchain.Evm.DebugTrace.Models;
using System.Text.Json.Serialization;

namespace Atria.Contracts.Events.Blockchain.Evm.DebugTrace;

public sealed record DebugTraces(
 [property: JsonPropertyName("metadata")]
 Metadata Metadata,
 [property: JsonPropertyName("traces")]
 List<DebugTraceData> Traces);
