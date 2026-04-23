using Atria.Core.Data.Entities.Enums;

namespace Atria.Core.Data.Entities.Outputs.Config;

public class TelegramOutputConfig : OutputConfigBase
{
    public override OutputType OutputType => OutputType.Telegram;

    public string BotToken { get; set; }

    public string ChatId { get; set; }

    public string MessageTemplate { get; set; }

    public bool EnableMarkdown { get; set; } = true;

    public bool DisableWebPagePreview { get; set; } = false;

    public bool DisableNotification { get; set; } = false;

    public int TimeoutSeconds { get; set; } = 30;
}
