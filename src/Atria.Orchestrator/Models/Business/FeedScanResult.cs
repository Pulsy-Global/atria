namespace Atria.Orchestrator.Models.Business;

public record ScanResult<T>
{
    public List<T> Added { get; init; } = new();
    public List<T> Modified { get; init; } = new();
    public List<Guid> RemovedIds { get; init; } = new();
}
