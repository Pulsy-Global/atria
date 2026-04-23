using Atria.Core.Data.Entities.Enums;

namespace Atria.Core.Data.Entities.Outputs.Config;

public class WebhookOutputConfig : OutputConfigBase
{
    public override OutputType OutputType => OutputType.Webhook;

    public string Url { get; set; }

    public WebhookHttpMethod Method { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new();

    public int TimeoutSeconds { get; set; } = 30;
}
