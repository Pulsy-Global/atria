namespace Atria.Orchestrator.Config.Options;

public class ProvisioningOptions
{
    public bool Enabled { get; set; } = false;
    public int PoolingIntervalSec { get; set; } = 30;
    public string Directory { get; set; } = "provisioning";
    public string FeedsDirectory { get; set; } = "feeds";
    public string OutputsDirectory { get; set; } = "connections";

    public string GetFeedsPath()
        => BuildPath(FeedsDirectory);

    public string GetOutputsPath()
        => BuildPath(OutputsDirectory);

    private string BuildPath(string directory)
    {
        if (string.IsNullOrWhiteSpace(Directory))
        {
            return directory;
        }

        return Path.Combine(Directory, directory);
    }
}
