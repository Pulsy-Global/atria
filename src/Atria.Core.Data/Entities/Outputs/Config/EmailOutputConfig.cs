using Atria.Core.Data.Entities.Enums;

namespace Atria.Core.Data.Entities.Outputs.Config;

public class EmailOutputConfig : OutputConfigBase
{
    public override OutputType OutputType => OutputType.Email;

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
