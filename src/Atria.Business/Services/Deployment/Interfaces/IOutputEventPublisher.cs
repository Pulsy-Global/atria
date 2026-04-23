namespace Atria.Business.Services.Deployment.Interfaces;

public interface IOutputEventPublisher
{
    Task PublishOutputUpdatedAsync(Guid outputId, CancellationToken ct = default);
}
