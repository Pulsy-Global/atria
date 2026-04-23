using Atria.Core.Business.Mapper.Attributes;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Outputs.Config;

namespace Atria.Core.Business.Models.Dto.Output.Config;

[ConfigMapping(OutputType.Discord, typeof(DiscordOutputConfig), typeof(DiscordDto))]
public class DiscordDto : ConfigBaseDto
{
    public string WebhookUrl { get; set; }

    public string Username { get; set; }

    public string AvatarUrl { get; set; }

    public string Message { get; set; }

    public bool EnableTts { get; set; } = false;

    public int TimeoutSeconds { get; set; } = 30;
}
