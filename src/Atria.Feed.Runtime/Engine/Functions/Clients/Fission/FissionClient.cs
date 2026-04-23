using Atria.Common.Exceptions;
using Atria.Common.Web.Models.Options;
using Atria.Feed.Runtime.Engine.Exceptions;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Interfaces;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Function;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Services;
using Atria.Feed.Runtime.Engine.Functions.Options;
using k8s;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission;

public class FissionClient : IFissionClient
{
    private readonly FissionKubernetesService _kubernetesService;
    private readonly FissionHttpService _httpService;
    private readonly FissionClientOptions _options;
    private readonly ILogger<FissionClient> _logger;

    public FissionClient(
        IOptions<FissionClientOptions> options,
        IOptions<FeaturesOptions> featuresOptions,
        ILogger<FissionClient> logger,
        IHttpClientFactory httpClientFactory,
        ILogger<FissionHttpService> httpLogger,
        ILogger<FissionKubernetesService> kubernetesLogger)
    {
        _options = options.Value;
        _logger = logger;

        if (!featuresOptions.Value.FunctionsEnabled)
        {
            _logger.LogInformation("Fission client is disabled (FunctionsEnabled=false)");
            return;
        }

        ValidateOptions();

        var kubeconfigPath = string.IsNullOrEmpty(_options.KubeConfigPath) ? null : _options.KubeConfigPath;

        var config = _options.InClusterConfig
            ? KubernetesClientConfiguration.InClusterConfig()
            : KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeconfigPath);

        var kubernetesClient = new Kubernetes(config);
        var httpClient = httpClientFactory.CreateClient("FissionClient");

        _kubernetesService = new FissionKubernetesService(kubernetesClient, kubernetesLogger);
        _httpService = new FissionHttpService(httpClient, _options, httpLogger);
    }

    public async Task<object?> InvokeFunctionAsync(string functionId, object? input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(functionId))
        {
            throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
        }

        using var scope = _logger.BeginScope("Invoking function {FunctionId}", functionId);

        try
        {
            return await _httpService.InvokeFunctionAsync(functionId, input, ct);
        }
        catch (FeedEngineException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke Fission function '{FunctionId}'", functionId);
            throw new InvalidOperationException($"Failed to invoke Fission function '{functionId}': {ex.Message}", ex);
        }
    }

    public async Task<bool> DeployFunctionAsync(FissionFunctionDeployment deployment, CancellationToken ct)
    {
        ValidateDeployment(deployment);

        using var scope = _logger.BeginScope("Deploying function {FunctionName}", deployment.Name);

        try
        {
            await CreatePackageAndFunctionAsync(deployment, ct);

            try
            {
                await _kubernetesService.CreateHttpTriggerAsync(deployment, _options.FunctionsNamespace, ct);
            }
            catch (Exception triggerEx)
            {
                _logger.LogError(triggerEx, "Failed to create HTTP trigger for function '{FunctionName}'. Function will be created without HTTP trigger.", deployment.Name);
            }

            var timeout = TimeSpan.FromSeconds(_options.FunctionReadyTimeoutSeconds);
            return await WaitForFunctionReadyAsync(deployment, timeout, ct);
        }
        catch (Exception ex) when (ex is not FeedEngineException)
        {
            _logger.LogError(ex, "Failed to deploy function '{FunctionName}'", deployment.Name);
            throw new InvalidOperationException($"Failed to deploy function '{deployment.Name}' via Kubernetes", ex);
        }
    }

    public async Task<bool> UpdateFunctionAsync(FissionFunctionDeployment deployment, CancellationToken ct)
    {
        ValidateDeployment(deployment);

        using var scope = _logger.BeginScope("Updating function {FunctionName}", deployment.Name);

        try
        {
            await _kubernetesService.UpdatePackageAsync(deployment, _options.FunctionsNamespace, ct);

            var timeout = TimeSpan.FromSeconds(_options.FunctionReadyTimeoutSeconds);
            return await WaitForFunctionReadyAsync(deployment, timeout, ct);
        }
        catch (k8s.Autorest.HttpOperationException ex)
            when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex) when (ex is not FeedEngineException)
        {
            _logger.LogError(ex, "Failed to update function '{FunctionName}'", deployment.Name);
            throw new ItemNotFoundException($"Failed to update function '{deployment.Name}' via Kubernetes");
        }
    }

    public async Task<bool> DeleteFunctionAsync(string functionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(functionId))
        {
            throw new ArgumentException("Function ID cannot be null or empty", nameof(functionId));
        }

        using var scope = _logger.BeginScope("Deleting function {FunctionId}", functionId);

        try
        {
            await _kubernetesService.DeleteHttpTriggerAsync(functionId, _options.FunctionsNamespace, ct);
            await _kubernetesService.DeleteFunctionAsync(functionId, _options.FunctionsNamespace, ct);
            await _kubernetesService.DeletePackageAsync(functionId, _options.FunctionsNamespace, ct);
            _logger.LogInformation("Successfully deleted function '{FunctionId}' with all dependencies", functionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Fission function '{FunctionId}'", functionId);
            throw new InvalidOperationException($"Failed to delete Fission function '{functionId}'", ex);
        }
    }

    public async Task<bool> WaitForFunctionReadyAsync(FissionFunctionDeployment deployment, TimeSpan timeout, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var kubernetesReady = await _kubernetesService.IsFunctionReadyAsync(deployment.Name, _options.FunctionsNamespace, ct);
                var httpReady = await _httpService.IsFunctionReadyViaHttpAsync(deployment.Name, ct);

                if (kubernetesReady && httpReady)
                {
                    _logger.LogInformation("Function '{FunctionName}' is ready", deployment.Name);
                    return true;
                }

                await Task.Delay(_options.ShortDelayMilliseconds, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking readiness for function '{FunctionName}'", deployment.Name);
                await Task.Delay(_options.LongDelayMilliseconds, ct);
            }
        }

        throw new TimeoutException($"Function '{deployment.Name}' not ready after {timeout.TotalSeconds} seconds");
    }

    private async Task CreatePackageAndFunctionAsync(FissionFunctionDeployment deployment, CancellationToken ct)
    {
        await _kubernetesService.CreatePackageAsync(deployment, _options.FunctionsNamespace, ct);
        await _kubernetesService.CreateFunctionAsync(deployment, _options.FunctionsNamespace, ct);
    }

    private void ValidateDeployment(FissionFunctionDeployment deployment)
    {
        if (deployment == null)
        {
            throw new ArgumentNullException(nameof(deployment));
        }

        if (string.IsNullOrWhiteSpace(deployment.Name))
        {
            throw new ArgumentException("Deployment name cannot be null or empty", nameof(deployment));
        }

        if (string.IsNullOrWhiteSpace(deployment.Environment))
        {
            throw new ArgumentException("Deployment environment cannot be null or empty", nameof(deployment));
        }

        if (string.IsNullOrWhiteSpace(deployment.Code))
        {
            throw new ArgumentException("Deployment code cannot be null or empty", nameof(deployment));
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new ArgumentException("BaseUrl cannot be null or empty", nameof(_options.BaseUrl));
        }

        if (string.IsNullOrWhiteSpace(_options.FunctionsNamespace))
        {
            throw new ArgumentException("FunctionsNamespace cannot be null or empty", nameof(_options.FunctionsNamespace));
        }
    }
}
