namespace Atria.Feed.Runtime.Engine.Functions.Interfaces;

public interface IFunctionContext
{
    Task<object?> ExecuteAsync(object? input, CancellationToken ct);

    Task RedeployAndWaitForReadyAsync(string code, CancellationToken ct);

    Task DeleteAsync(CancellationToken ct);
}
