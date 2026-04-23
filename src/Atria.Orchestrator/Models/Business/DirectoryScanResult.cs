namespace Atria.Orchestrator.Models.Business;

public class DirectoryScanResult<T>
{
    public string DirectoryName { get; set; }
    public string FileHash { get; set; }
    public T Item { get; set; }
}
