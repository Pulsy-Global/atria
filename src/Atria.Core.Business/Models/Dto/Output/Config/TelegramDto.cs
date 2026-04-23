using Atria.Core.Business.Mapper.Attributes;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Outputs.Config;

namespace Atria.Core.Business.Models.Dto.Output.Config;

[ConfigMapping(OutputType.Telegram, typeof(TelegramOutputConfig), typeof(TelegramDto))]
public class TelegramDto : ConfigBaseDto
{
    public string BotToken { get; set; }

    public string ChatId { get; set; }

    public string MessageTemplate { get; set; }

    public bool EnableMarkdown { get; set; } = true;

    public bool DisableWebPagePreview { get; set; } = false;

    public bool DisableNotification { get; set; } = false;

    public int TimeoutSeconds { get; set; } = 30;
}
