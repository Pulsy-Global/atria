namespace Atria.Common.Models.Options;

public class NetworkGroupOptions
{
    public string Title { get; set; }

    public string IconUrl { get; set; }

    public bool Disabled { get; set; }

    public List<NetworkOptions> Environments { get; set; } = [];
}
