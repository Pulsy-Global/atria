namespace Atria.Core.Business.Services.Probe;

public enum WebhookProbeFailureReason
{
    Ssrf,
    Dns,
    Timeout,
    NotFound,
    RedirectAttempted,
    Unreachable,
}
