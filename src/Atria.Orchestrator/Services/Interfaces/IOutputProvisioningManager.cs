namespace Atria.Orchestrator.Services.Interfaces;

public interface IOutputProvisioningManager
{
    Task ExecuteProvisioningAsync(CancellationToken ct = default);
}
