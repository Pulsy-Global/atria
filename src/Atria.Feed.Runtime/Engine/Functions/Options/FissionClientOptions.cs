using System.ComponentModel.DataAnnotations;

namespace Atria.Feed.Runtime.Engine.Functions.Options;

public class FissionClientOptions
{
    [Required(ErrorMessage = "BaseUrl is required")]
    [Url(ErrorMessage = "BaseUrl must be a valid URL")]
    public string BaseUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "FunctionsNamespace is required")]
    public string FunctionsNamespace { get; set; } = "fission-function";

    public int ShortDelayMilliseconds { get; set; } = 500;

    public int LongDelayMilliseconds { get; set; } = 2000;

    public int FunctionReadyTimeoutSeconds { get; set; } = 20;

    public int HttpClientTimeoutSeconds { get; set; } = 30;

    public bool InClusterConfig { get; set; }

    public string? KubeConfigPath { get; set; }
}
