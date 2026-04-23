using Atria.Core.Data.Entities.Enums;

namespace Atria.Orchestrator.Models.Dto.Outputs;

public class FeedOutputConfig
{
    public string Id { get; init; }

    public string Name { get; init; }

    public OutputType Type { get; init; }

    public dynamic Config { get; init; }
}
