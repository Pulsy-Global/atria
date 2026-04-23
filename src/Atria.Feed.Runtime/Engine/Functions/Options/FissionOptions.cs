using System.ComponentModel.DataAnnotations;

namespace Atria.Feed.Runtime.Engine.Functions.Options;

public class FissionOptions
{
    [Required(ErrorMessage = "EnabledRuntimes is required")]
    public List<string> EnabledRuntimes { get; set; } = new();

    public Dictionary<string, RuntimeConfigOptions> RuntimeConfig { get; set; } = new();
}
