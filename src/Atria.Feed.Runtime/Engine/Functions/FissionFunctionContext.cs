using Atria.Contracts.Events.Feed.Enums;
using Atria.Feed.Runtime.Engine.Exceptions;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Interfaces;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Function;
using Atria.Feed.Runtime.Engine.Functions.Interfaces;
using Atria.Feed.Runtime.Engine.Functions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Atria.Feed.Runtime.Engine.Functions;

public class FissionFunctionContext : IFunctionContext
{
    private static readonly string _baseDir = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string _wrappersPath = Path.Combine(_baseDir, "Scripts", "Functions", "Fission");

    private readonly IFissionClient _client;
    private readonly RuntimeConfigOptions _options;

    private readonly string _functionId;
    private readonly string _environment;

    public FissionFunctionContext(
        IServiceScopeFactory serviceScope,
        string functionId,
        FunctionLangKind langKind,
        IFissionClient client)
    {
        using var scope = serviceScope.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<IOptions<FissionOptions>>();

        _functionId = functionId ?? throw new ArgumentNullException(nameof(functionId));
        _client = client ?? throw new ArgumentNullException(nameof(client));

        var environment = options.Value.RuntimeConfig
            .FirstOrDefault(x => x.Value.Language == langKind);

        if (!options.Value.EnabledRuntimes.Contains(environment.Key))
        {
            throw new FeedEngineException($"Environment {environment} is not supported", string.Empty);
        }

        _environment = environment.Key;
        _options = environment.Value ?? throw new FeedEngineException($"Environment not found for {langKind.ToString()}", string.Empty);
    }

    public async Task<object?> ExecuteAsync(object? input, CancellationToken ct)
    {
        return await _client.InvokeFunctionAsync(_functionId, input, ct);
    }

    public async Task RedeployAndWaitForReadyAsync(string code, CancellationToken ct = default)
    {
        var path = Path.Combine(_wrappersPath, _environment, _options.WrapperFile);
        var wrapperContent = await File.ReadAllTextAsync(path, ct);

        var deployment = new FissionFunctionDeployment
        {
            Name = _functionId,
            Code = $"{code + Environment.NewLine + wrapperContent}",
            Environment = _environment,
        };

        var isUpdated = await _client.UpdateFunctionAsync(deployment, ct);
        if (!isUpdated)
        {
            await _client.DeployFunctionAsync(deployment, ct);
        }
    }

    public async Task DeleteAsync(CancellationToken ct)
    {
        await _client.DeleteFunctionAsync(_functionId, CancellationToken.None);
    }
}
