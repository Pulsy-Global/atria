using Atria.Common.Exceptions;

namespace Atria.Core.Business.Services.Probe;

public class WebhookProbeException : ValidationException
{
    private const string UrlFieldKey = "config.url";

    public WebhookProbeFailureReason Reason { get; }

    public WebhookProbeException(WebhookProbeFailureReason reason, string message)
        : base(new Dictionary<string, string[]>
        {
            [UrlFieldKey] = [message],
        })
    {
        Reason = reason;
    }
}
