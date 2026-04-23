using Atria.Core.Data.Entities.Enums;

namespace Atria.Core.Data.Entities.Outputs.Config;

public class DiscordOutputConfig : OutputConfigBase
{
    public override OutputType OutputType => OutputType.Discord;

    public string WebhookUrl { get; set; }

    public string Username { get; set; }

    public string AvatarUrl { get; set; }

    public string Message { get; set; }

    public bool EnableTts { get; set; } = false;

    public int TimeoutSeconds { get; set; } = 30;
}
