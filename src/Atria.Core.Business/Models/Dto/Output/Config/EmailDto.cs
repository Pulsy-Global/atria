using Atria.Core.Business.Mapper.Attributes;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Outputs.Config;

namespace Atria.Core.Business.Models.Dto.Output.Config;

[ConfigMapping(OutputType.Email, typeof(EmailOutputConfig), typeof(EmailDto))]
public class EmailDto : ConfigBaseDto
{
    public string SmtpServer { get; set; }

    public int SmtpPort { get; set; } = 587;

    public string Username { get; set; }

    public string Password { get; set; }

    public bool EnableSsl { get; set; } = true;

    public string FromEmail { get; set; }

    public string FromName { get; set; }

    public List<string> ToEmails { get; set; } = new();

    public List<string> CcEmails { get; set; } = new();

    public List<string> BccEmails { get; set; } = new();

    public string Subject { get; set; }

    public string BodyTemplate { get; set; }

    public bool IsHtml { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 30;
}
