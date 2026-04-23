using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Function;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Interfaces;

public interface IFissionClient
{
    Task<object?> InvokeFunctionAsync(string functionId, object? input, CancellationToken ct);

    Task<bool> DeployFunctionAsync(FissionFunctionDeployment deployment, CancellationToken ct);

    Task<bool> UpdateFunctionAsync(FissionFunctionDeployment deployment, CancellationToken ct);

    Task<bool> DeleteFunctionAsync(string functionId, CancellationToken ct);
}
