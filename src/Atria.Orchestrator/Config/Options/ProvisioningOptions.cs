namespace Atria.Orchestrator.Config.Options;

public class ProvisioningOptions
{
    public bool Enabled { get; set; } = false;
    public int PoolingIntervalSec { get; set; } = 30;
    public string FeedsDirectory { get; set; } = "feeds";
    public string OutputsDirectory { get; set; } = "connections";
}
