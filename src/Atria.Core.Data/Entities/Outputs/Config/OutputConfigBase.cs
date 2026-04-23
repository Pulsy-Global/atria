using Atria.Core.Data.Entities.Enums;
using System.Text.Json.Serialization;

namespace Atria.Core.Data.Entities.Outputs.Config;

public abstract class OutputConfigBase
{
    [JsonPropertyName("outputType")]
    public abstract OutputType OutputType { get; }
}
