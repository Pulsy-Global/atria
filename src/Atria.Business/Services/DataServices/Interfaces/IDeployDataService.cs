using Atria.Business.Models;
using Atria.Core.Data.Entities.Deploys;
using System.Linq.Expressions;

namespace Atria.Business.Services.DataServices.Interfaces;

public interface IDeployDataService
{
    Task<List<Deploy>> GetDeploysAsync(Expression<Func<Deploy, bool>> predicate, CancellationToken ct);

    Task<Deploy> ExecuteDeploymentAsync(Guid feedId, CancellationToken ct);

    Task PublishDeployRequestAsync(Guid feedId, CancellationToken ct);

    Task PauseFromRuntimeAsync(Guid feedId, CancellationToken ct);

    Task DeleteFromRuntimeAsync(Guid feedId, CancellationToken ct);

    Task<Deploy> UpdateDeployAsync(Deploy deploy, CancellationToken ct);

    Task<Deploy?> GetCurrentDeployAsync(Guid feedId, CancellationToken ct);

    Task ConfirmDeployedAsync(Guid feedId, CancellationToken ct);

    Task<TestResult> TestFeedDeployAsync(TestRequest testRequest, CancellationToken ct);
}
