using Atria.Core.Business.Mapper.Attributes;
using Atria.Core.Business.Models.Dto.Output.Config.Attributes;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Outputs.Config;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Business.Models.Dto.Output.Config;

[ConfigMapping(OutputType.Webhook, typeof(WebhookOutputConfig), typeof(WebhookDto))]
public class WebhookDto : ConfigBaseDto
{
    [Required]
    [MaxLength(2048)]
    [RegularExpression(@"^https?://.+", ErrorMessage = "URL must start with http:// or https://")]
    public string Url { get; set; }

    [EnumDataType(typeof(WebhookHttpMethod))]
    public WebhookHttpMethod Method { get; set; }

    [MaxHeadersLength]
    public Dictionary<string, string> Headers { get; set; } = new();

    [Range(1, 45)]
    public int TimeoutSeconds { get; set; } = 10;
}
