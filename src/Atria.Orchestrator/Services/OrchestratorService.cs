using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Atria.Orchestrator.Services;

public sealed class OrchestratorService : BackgroundService
{
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        ILogger<OrchestratorService> logger)
    {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Orchestrator service started");
        return Task.CompletedTask;
    }
}
