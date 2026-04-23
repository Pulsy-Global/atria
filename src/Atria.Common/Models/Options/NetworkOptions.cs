namespace Atria.Common.Models.Options;

public class NetworkOptions
{
    public string Id { get; set; }
    public string NodeRpcUrl { get; set; }
    public string NodeWsUrl { get; set; }
    public string Environment { get; set; }
    public string Title { get; set; }
    public bool DebugRequestsEnabled { get; set; } = true;
    public bool Disabled { get; set; }
}
