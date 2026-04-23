namespace Atria.Orchestrator.Config.Options;

public class OrchestratorOptions
{
    public bool Enabled { get; set; } = true;
    public int DeployStuckIntervalSec { get; set; }
    public int DeployRetryIntervalSec { get; set; } = 30;
    public ProvisioningOptions Provisioning { get; set; }
}
