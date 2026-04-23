using Atria.Core.Business.Models.Dto.Output.Config;

namespace Atria.Core.Business.Services.Probe;

public interface IWebhookProbeService
{
    Task ProbeAsync(WebhookDto config, CancellationToken ct);
}
